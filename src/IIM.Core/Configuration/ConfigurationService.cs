using IIM.Shared.Interfaces;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace IIM.Core.Configuration
{

    public class ConfigDbContext : DbContext
    {
        public ConfigDbContext(DbContextOptions<ConfigDbContext> options) : base(options) { }

        // Example audit entity:
        public DbSet<Setting> Settings { get; set; }
    }


    public class ConfigurationService : IConfigurationService
    {
        private readonly IConfiguration _staticConfig;
        private readonly ConfigDbContext _dbContext;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ConfigurationService> _logger;
        private readonly IAuditLogger _auditlogger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ConfigurationService(
            IConfiguration staticConfig,
            ConfigDbContext dbContext,
            IMemoryCache cache,
            ILogger<ConfigurationService> logger,
            IAuditLogger auditlogger, IHttpContextAccessor httpContextAccessor )
        {
            _staticConfig = staticConfig;
            _dbContext = dbContext;
            _cache = cache;
            _logger = logger;
            _auditlogger = auditlogger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<T> GetSettingAsync<T>(string key, T defaultValue = default)
        {
           

            // Check environment variable
            var envKey = $"IIM_{key.Replace(':', '_').ToUpper()}";
            var envValue = Environment.GetEnvironmentVariable(envKey);
            if (!string.IsNullOrEmpty(envValue))
            {
                return ConvertValue<T>(envValue);
            }

            // Check cache
            if (_cache.TryGetValue(key, out T cachedValue))
            {
                return cachedValue;
            }

            // Check database
            var dbSetting = await _dbContext.Settings
                .FirstOrDefaultAsync(s => s.Key == key);

            if (dbSetting != null)
            {
                var value = JsonSerializer.Deserialize<T>(dbSetting.Value);
                _cache.Set(key, value, TimeSpan.FromMinutes(5));
                return value;
            }

            // Check static config
            var configValue = _staticConfig.GetValue<T>(key);
            if (configValue != null)
            {
                return configValue;
            }

            return defaultValue;
        }

        public async Task SetSettingAsync<T>(string key, T value)
        {
            var setting = await _dbContext.Settings
                .FirstOrDefaultAsync(s => s.Key == key);

            if (setting == null)
            {
                setting = new Setting
                {
                    Key = key,
                    Value = JsonSerializer.Serialize(value),
                    UpdatedAt = DateTimeOffset.UtcNow,
                    UpdatedBy = GetCurrentUser()
                };
                _dbContext.Settings.Add(setting);
            }
            else
            {
                setting.Value = JsonSerializer.Serialize(value);
                setting.UpdatedAt = DateTimeOffset.UtcNow;
                setting.UpdatedBy = GetCurrentUser();
            }

            await _dbContext.SaveChangesAsync();

            // Clear cache
            _cache.Remove(key);

            // Audit the change
            await AuditSettingChangeAsync(key, value);
        }

        public async Task<Dictionary<string, object>> GetAllSettingsAsync()
        {
            var settings = new Dictionary<string, object>();

            // Get all database settings
            var dbSettings = await _dbContext.Settings.ToListAsync();

            foreach (var setting in dbSettings)
            {
                settings[setting.Key] = JsonSerializer.Deserialize<object>(setting.Value);
            }

            return settings;
        }

        public async Task ReloadSettingsAsync()
        {
            // Clear the cache to force reload from database
            if (_cache is MemoryCache memCache)
            {
                memCache.Clear();
            }

            _logger.LogInformation("Configuration cache cleared, settings will be reloaded");
            await Task.CompletedTask;
        }

        private T ConvertValue<T>(string value)
        {
            if (typeof(T) == typeof(string))
                return (T)(object)value;

            if (typeof(T) == typeof(bool))
                return (T)(object)bool.Parse(value);

            if (typeof(T) == typeof(int))
                return (T)(object)int.Parse(value);

            // For complex types, deserialize from JSON
            return JsonSerializer.Deserialize<T>(value);
        }

        private string GetCurrentUser()
        {
            // Get from HttpContext or return system user
            return _httpContextAccessor?.HttpContext?.User?.Identity?.Name ?? "system";
        }

        private async Task AuditSettingChangeAsync<T>(string key, T value)
        {
             _auditlogger.LogAuditEvent(new AuditEvent
            {
                Action = "SETTING_CHANGED",
                EntityType = "Setting",
                EntityId = key,
                Details = JsonSerializer.Serialize(new
                {
                    Key = key,
                    NewValue = value
                })

            });
        }
    }
}
