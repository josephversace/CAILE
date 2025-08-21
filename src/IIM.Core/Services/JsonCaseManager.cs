
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IIM.Core.Configuration;
using IIM.Core.Models;
using IIM.Infrastructure.Storage;
using IIM.Shared.Enums;
using IIM.Shared.Models;
using Microsoft.Extensions.Logging;

namespace IIM.Core.Services
{
    /// <summary>
    /// JSON file-based implementation of ICaseManager
    /// </summary>
    public class JsonCaseManager : ICaseManager
    {
        private readonly ILogger<JsonCaseManager> _logger;
        private readonly StorageConfiguration _config;
        private readonly Dictionary<string, Case> _caseCache = new();
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Initializes a new instance of JsonCaseManager
        /// </summary>
        public JsonCaseManager(ILogger<JsonCaseManager> logger, StorageConfiguration config)
        {
            _logger = logger;
            _config = config;
            _config.EnsureDirectoriesExist();
            LoadCasesIntoCache();
        }

        /// <summary>
        /// Creates a new case and saves it to JSON storage
        /// </summary>
        public async Task<Case> CreateCaseAsync(string name, string description, CaseType type,
            CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                var caseEntity = new Case
                {
                    Id = Guid.NewGuid().ToString("N"),
                    CaseNumber = GenerateCaseNumber(),
                    Name = name,
                    Description = description,
                    Type = type,
                    Status = CaseStatus.Open,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    Priority = CasePriority.Medium,
                    LeadInvestigator = Environment.UserName,
                    TeamMembers = new List<string> { Environment.UserName },
                    Evidence = new List<Evidence>(),
                    Sessions = new List<InvestigationSession>(),
                    Timelines = new List<Timeline>(),
                    Reports = new List<Report>(),
                    Metadata = new Dictionary<string, object>(),
                    Classification = "UNCLASSIFIED",
                    AccessControlList = new List<string> { Environment.UserName }
                };

                await SaveCaseToJsonAsync(caseEntity);
                _caseCache[caseEntity.Id] = caseEntity;

                _logger.LogInformation("Created case {CaseId} with number {CaseNumber}",
                    caseEntity.Id, caseEntity.CaseNumber);

                return caseEntity;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Retrieves a case from JSON storage or cache
        /// </summary>
        public async Task<Case?> GetCaseAsync(string caseId, CancellationToken cancellationToken = default)
        {
            // Check cache first
            if (_caseCache.TryGetValue(caseId, out var cachedCase))
            {
                return cachedCase;
            }

            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                var filePath = GetCaseFilePath(caseId);
                if (!File.Exists(filePath))
                {
                    return null;
                }

                var json = await File.ReadAllTextAsync(filePath, cancellationToken);
                var caseEntity = JsonSerializer.Deserialize<Case>(json, _jsonOptions);

                if (caseEntity != null)
                {
                    _caseCache[caseId] = caseEntity;
                }

                return caseEntity;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Retrieves all cases from JSON storage
        /// </summary>
        public async Task<List<Case>> GetUserCasesAsync(string? userId = null,
            CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                var cases = _caseCache.Values.ToList();

                if (!string.IsNullOrEmpty(userId))
                {
                    cases = cases.Where(c =>
                        c.LeadInvestigator == userId ||
                        c.TeamMembers.Contains(userId) ||
                        c.AccessControlList.Contains(userId))
                        .ToList();
                }

                return cases.OrderByDescending(c => c.UpdatedAt).ToList();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Updates a case and saves changes to JSON storage
        /// </summary>
        public async Task<bool> UpdateCaseAsync(string caseId, Action<Case> updateAction,
            CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                var caseEntity = await GetCaseAsync(caseId, cancellationToken);
                if (caseEntity == null)
                {
                    return false;
                }

                updateAction(caseEntity);
                caseEntity.UpdatedAt = DateTimeOffset.UtcNow;

                await SaveCaseToJsonAsync(caseEntity);
                _caseCache[caseId] = caseEntity;

                return true;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Links an investigation session to a case
        /// </summary>
        public async Task<bool> LinkSessionToCaseAsync(string sessionId, string caseId,
            CancellationToken cancellationToken = default)
        {
            return await UpdateCaseAsync(caseId, c =>
            {
                // Sessions is a list of InvestigationSession objects, not strings
                // So we need to create a minimal session or track IDs differently
                c.Metadata[$"session_{sessionId}"] = DateTimeOffset.UtcNow;
            }, cancellationToken);
        }

        /// <summary>
        /// Links evidence to a case
        /// </summary>
        public async Task<bool> LinkEvidenceToCaseAsync(string evidenceId, string caseId,
            CancellationToken cancellationToken = default)
        {
            return await UpdateCaseAsync(caseId, c =>
            {
                // Evidence is a list of Evidence objects, not strings
                // So we track the ID in metadata
                c.Metadata[$"evidence_{evidenceId}"] = DateTimeOffset.UtcNow;
            }, cancellationToken);
        }

        /// <summary>
        /// Gets the most recently updated cases
        /// </summary>
        public async Task<List<Case>> GetRecentCasesAsync(int count = 10,
            CancellationToken cancellationToken = default)
        {
            var cases = await GetUserCasesAsync(null, cancellationToken);
            return cases.Take(count).ToList();
        }

        /// <summary>
        /// Soft deletes a case by marking it as deleted
        /// </summary>
        public async Task<bool> DeleteCaseAsync(string caseId, CancellationToken cancellationToken = default)
        {
            return await UpdateCaseAsync(caseId, c =>
            {
                c.Status = CaseStatus.Closed;
                c.Metadata["DeletedAt"] = DateTimeOffset.UtcNow;
                c.Metadata["IsDeleted"] = true;
            }, cancellationToken);
        }

        // Private helper methods

        /// <summary>
        /// Saves a case to JSON file
        /// </summary>
        private async Task SaveCaseToJsonAsync(Case caseEntity)
        {
            var filePath = GetCaseFilePath(caseEntity.Id);
            var json = JsonSerializer.Serialize(caseEntity, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json);
        }

        /// <summary>
        /// Gets the file path for a case
        /// </summary>
        private string GetCaseFilePath(string caseId)
        {
            return Path.Combine(_config.CasesPath, $"{caseId}.json");
        }

        /// <summary>
        /// Generates a unique case number
        /// </summary>
        private string GenerateCaseNumber()
        {
            var year = DateTime.UtcNow.Year;
            var month = DateTime.UtcNow.Month;
            var count = _caseCache.Count + 1;
            return $"IIM-{year:0000}-{month:00}-{count:00000}";
        }

        /// <summary>
        /// Loads all cases from JSON files into cache
        /// </summary>
        private void LoadCasesIntoCache()
        {
            if (!Directory.Exists(_config.CasesPath))
            {
                return;
            }

            foreach (var file in Directory.GetFiles(_config.CasesPath, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var caseEntity = JsonSerializer.Deserialize<Case>(json, _jsonOptions);
                    if (caseEntity != null)
                    {
                        _caseCache[caseEntity.Id] = caseEntity;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load case from {File}", file);
                }
            }

            _logger.LogInformation("Loaded {Count} cases into cache", _caseCache.Count);
        }
    }
}