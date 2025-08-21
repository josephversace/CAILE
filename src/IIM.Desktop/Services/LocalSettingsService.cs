using IIM.Desktop;
using IIM.Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace IIM.Desktop.Services
{
    /// <summary>
    /// Implementation of local settings service using JSON file storage.
    /// Stores UI preferences and user settings locally.
    /// </summary>
    public class LocalSettingsService : ILocalSettingsService
    {
        private readonly string _settingsPath;

        /// <summary>
        /// Initializes a new instance of LocalSettingsService.
        /// Creates settings file path in user's local app data.
        /// </summary>
        public LocalSettingsService()
        {
            _settingsPath = Path.Combine(Program.GetLocalSettingsDirectory(), "ui-settings.json");
        }

        /// <summary>
        /// Retrieves a setting value from the local JSON file.
        /// </summary>
        /// <typeparam name="T">Type to deserialize the setting value to</typeparam>
        /// <param name="key">Setting key to retrieve</param>
        /// <returns>Deserialized setting value or default if not found</returns>
        public async Task<T?> GetAsync<T>(string key)
        {
            if (!File.Exists(_settingsPath))
                return default;

            var json = await File.ReadAllTextAsync(_settingsPath);
            var settings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

            if (settings?.TryGetValue(key, out var element) == true)
            {
                return element.Deserialize<T>();
            }

            return default;
        }

        /// <summary>
        /// Stores a setting value in the local JSON file.
        /// Creates the file if it doesn't exist.
        /// </summary>
        /// <typeparam name="T">Type of the value to store</typeparam>
        /// <param name="key">Setting key</param>
        /// <param name="value">Value to store</param>
        public async Task SetAsync<T>(string key, T value)
        {
            var settings = new Dictionary<string, object>();

            if (File.Exists(_settingsPath))
            {
                var json = await File.ReadAllTextAsync(_settingsPath);
                settings = JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new();
            }

            settings[key] = value;

            var updatedJson = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_settingsPath, updatedJson);
        }

        /// <summary>
        /// Removes a setting from the local JSON file.
        /// </summary>
        /// <param name="key">Setting key to remove</param>
        public async Task RemoveAsync(string key)
        {
            if (!File.Exists(_settingsPath))
                return;

            var json = await File.ReadAllTextAsync(_settingsPath);
            var settings = JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new();

            if (settings.Remove(key))
            {
                var updatedJson = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_settingsPath, updatedJson);
            }
        }
    }
}
