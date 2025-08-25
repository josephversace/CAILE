using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Models;
using Dapper; 
using IIM.Core.Configuration;
using IIM.Core.Models;
using IIM.Infrastructure.Storage;
using IIM.Shared.Enums;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace IIM.Core.Services
{
    /// <summary>
    /// SQLite database implementation of ICaseManager
    /// </summary>
    public class SqliteCaseManager : ICaseManager
    {
        private readonly ILogger<SqliteCaseManager> _logger;
        private readonly StorageConfiguration _config;
        private readonly string _connectionString;

        /// <summary>
        /// Initializes a new instance of SqliteCaseManager
        /// </summary>
        public SqliteCaseManager(ILogger<SqliteCaseManager> logger, StorageConfiguration config)
        {
            _logger = logger;
            _config = config;
            _config.EnsureDirectoriesExist();
            _connectionString = $"Data Source={_config.SqlitePath}";
            InitializeDatabase().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Creates a new case in the SQLite database
        /// </summary>
        public async Task<Case> CreateCaseAsync(string name, string description, CaseType type,
            CancellationToken cancellationToken = default)
        {
            var caseEntity = new Case
            {
                Id = Guid.NewGuid().ToString("N"),
                CaseNumber = await GenerateCaseNumberAsync(),
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

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string sql = @"
                INSERT INTO Cases (
                    Id, CaseNumber, Name, Type, Status, Description,
                    LeadInvestigator, TeamMembers, CreatedAt, UpdatedAt,
                    Priority, Classification, AccessControlList, Metadata
                ) VALUES (
                    @Id, @CaseNumber, @Name, @Type, @Status, @Description,
                    @LeadInvestigator, @TeamMembersJson, @CreatedAt, @UpdatedAt,
                    @Priority, @Classification, @AccessControlListJson, @MetadataJson
                )";

            await connection.ExecuteAsync(sql, new
            {
                caseEntity.Id,
                caseEntity.CaseNumber,
                caseEntity.Name,
                Type = type.ToString(),
                Status = caseEntity.Status.ToString(),
                caseEntity.Description,
                caseEntity.LeadInvestigator,
                TeamMembersJson = JsonSerializer.Serialize(caseEntity.TeamMembers),
                caseEntity.CreatedAt,
                caseEntity.UpdatedAt,
                Priority = caseEntity.Priority.ToString(),
                caseEntity.Classification,
                AccessControlListJson = JsonSerializer.Serialize(caseEntity.AccessControlList),
                MetadataJson = JsonSerializer.Serialize(caseEntity.Metadata)
            });

            _logger.LogInformation("Created case {CaseId} with number {CaseNumber}",
                caseEntity.Id, caseEntity.CaseNumber);

            return caseEntity;
        }

        /// <summary>
        /// Retrieves a case from the SQLite database
        /// </summary>
        public async Task<Case?> GetCaseAsync(string caseId, CancellationToken cancellationToken = default)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string sql = @"
                SELECT * FROM Cases WHERE Id = @CaseId AND IsDeleted = 0";

            var row = await connection.QueryFirstOrDefaultAsync(sql, new { CaseId = caseId });

            if (row == null)
            {
                return null;
            }

            return MapRowToCase(row);
        }

        /// <summary>
        /// Retrieves all cases from the SQLite database
        /// </summary>
        public async Task<List<Case>> GetUserCasesAsync(string? userId = null,
            CancellationToken cancellationToken = default)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            string sql = @"
                SELECT * FROM Cases 
                WHERE IsDeleted = 0";

            if (!string.IsNullOrEmpty(userId))
            {
                sql += @" AND (LeadInvestigator = @UserId 
                         OR TeamMembers LIKE @UserPattern
                         OR AccessControlList LIKE @UserPattern)";
            }

            sql += " ORDER BY UpdatedAt DESC";

            var rows = await connection.QueryAsync(sql, new
            {
                UserId = userId,
                UserPattern = $"%\"{userId}\"%"
            });

            return rows.Select(MapRowToCase).ToList();
        }

        /// <summary>
        /// Updates a case in the SQLite database
        /// </summary>
        public async Task<bool> UpdateCaseAsync(string caseId, Action<Case> updateAction,
            CancellationToken cancellationToken = default)
        {
            var caseEntity = await GetCaseAsync(caseId, cancellationToken);
            if (caseEntity == null)
            {
                return false;
            }

            updateAction(caseEntity);
            caseEntity.UpdatedAt = DateTimeOffset.UtcNow;

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string sql = @"
                UPDATE Cases SET
                    Name = @Name,
                    Description = @Description,
                    Status = @Status,
                    UpdatedAt = @UpdatedAt,
                    Priority = @Priority,
                    LeadInvestigator = @LeadInvestigator,
                    TeamMembers = @TeamMembersJson,
                    Classification = @Classification,
                    AccessControlList = @AccessControlListJson,
                    Metadata = @MetadataJson
                WHERE Id = @Id";

            var rowsAffected = await connection.ExecuteAsync(sql, new
            {
                caseEntity.Id,
                caseEntity.Name,
                caseEntity.Description,
                Status = caseEntity.Status.ToString(),
                caseEntity.UpdatedAt,
                Priority = caseEntity.Priority.ToString(),
                caseEntity.LeadInvestigator,
                TeamMembersJson = JsonSerializer.Serialize(caseEntity.TeamMembers),
                caseEntity.Classification,
                AccessControlListJson = JsonSerializer.Serialize(caseEntity.AccessControlList),
                MetadataJson = JsonSerializer.Serialize(caseEntity.Metadata)
            });

            return rowsAffected > 0;
        }

        /// <summary>
        /// Links a session to a case
        /// </summary>
        public async Task<bool> LinkSessionToCaseAsync(string sessionId, string caseId,
            CancellationToken cancellationToken = default)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string sql = @"
                INSERT INTO CaseSessions (CaseId, SessionId, LinkedAt)
                VALUES (@CaseId, @SessionId, @LinkedAt)";

            var rowsAffected = await connection.ExecuteAsync(sql, new
            {
                CaseId = caseId,
                SessionId = sessionId,
                LinkedAt = DateTimeOffset.UtcNow
            });

            return rowsAffected > 0;
        }

        /// <summary>
        /// Links evidence to a case
        /// </summary>
        public async Task<bool> LinkEvidenceToCaseAsync(string evidenceId, string caseId,
            CancellationToken cancellationToken = default)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string sql = @"
                INSERT INTO CaseEvidence (CaseId, EvidenceId, LinkedAt)
                VALUES (@CaseId, @EvidenceId, @LinkedAt)";

            var rowsAffected = await connection.ExecuteAsync(sql, new
            {
                CaseId = caseId,
                EvidenceId = evidenceId,
                LinkedAt = DateTimeOffset.UtcNow
            });

            return rowsAffected > 0;
        }

        /// <summary>
        /// Gets recent cases
        /// </summary>
        public async Task<List<Case>> GetRecentCasesAsync(int count = 10,
            CancellationToken cancellationToken = default)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string sql = @"
                SELECT * FROM Cases 
                WHERE IsDeleted = 0
                ORDER BY UpdatedAt DESC
                LIMIT @Count";

            var rows = await connection.QueryAsync(sql, new { Count = count });
            return rows.Select(MapRowToCase).ToList();
        }

        /// <summary>
        /// Soft deletes a case
        /// </summary>
        public async Task<bool> DeleteCaseAsync(string caseId, CancellationToken cancellationToken = default)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string sql = @"
                UPDATE Cases 
                SET IsDeleted = 1, UpdatedAt = @UpdatedAt
                WHERE Id = @CaseId";

            var rowsAffected = await connection.ExecuteAsync(sql, new
            {
                CaseId = caseId,
                UpdatedAt = DateTimeOffset.UtcNow
            });

            return rowsAffected > 0;
        }

        // Private helper methods

        /// <summary>
        /// Initializes the SQLite database schema
        /// </summary>
        private async Task InitializeDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            const string createCasesTable = @"
                CREATE TABLE IF NOT EXISTS Cases (
                    Id TEXT PRIMARY KEY,
                    CaseNumber TEXT NOT NULL UNIQUE,
                    Name TEXT NOT NULL,
                    Type TEXT NOT NULL,
                    Status TEXT NOT NULL,
                    Description TEXT,
                    LeadInvestigator TEXT,
                    TeamMembers TEXT,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL,
                    Priority TEXT,
                    Classification TEXT,
                    AccessControlList TEXT,
                    Metadata TEXT,
                    IsDeleted INTEGER DEFAULT 0
                );

                CREATE INDEX IF NOT EXISTS idx_cases_status ON Cases(Status);
                CREATE INDEX IF NOT EXISTS idx_cases_updated ON Cases(UpdatedAt);
                CREATE INDEX IF NOT EXISTS idx_cases_lead ON Cases(LeadInvestigator);";

            await connection.ExecuteAsync(createCasesTable);

            const string createLinkTables = @"
                CREATE TABLE IF NOT EXISTS CaseSessions (
                    CaseId TEXT NOT NULL,
                    SessionId TEXT NOT NULL,
                    LinkedAt TEXT NOT NULL,
                    PRIMARY KEY (CaseId, SessionId),
                    FOREIGN KEY (CaseId) REFERENCES Cases(Id)
                );

                CREATE TABLE IF NOT EXISTS CaseEvidence (
                    CaseId TEXT NOT NULL,
                    EvidenceId TEXT NOT NULL,
                    LinkedAt TEXT NOT NULL,
                    PRIMARY KEY (CaseId, EvidenceId),
                    FOREIGN KEY (CaseId) REFERENCES Cases(Id)
                );";

            await connection.ExecuteAsync(createLinkTables);

            _logger.LogInformation("SQLite database initialized at {Path}", _config.SqlitePath);
        }

        /// <summary>
        /// Maps a database row to a Case object
        /// </summary>
        private Case MapRowToCase(dynamic row)
        {
            return new Case
            {
                Id = row.Id,
                CaseNumber = row.CaseNumber,
                Name = row.Name,
                Type = Enum.Parse<CaseType>(row.Type),
                Status = Enum.Parse<CaseStatus>(row.Status),
                Description = row.Description,
                LeadInvestigator = row.LeadInvestigator,
                TeamMembers = string.IsNullOrEmpty(row.TeamMembers)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(row.TeamMembers) ?? new List<string>(),
                CreatedAt = DateTimeOffset.Parse(row.CreatedAt),
                UpdatedAt = DateTimeOffset.Parse(row.UpdatedAt),
                Priority = Enum.Parse<CasePriority>(row.Priority),
                Classification = row.Classification,
                AccessControlList = string.IsNullOrEmpty(row.AccessControlList)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(row.AccessControlList) ?? new List<string>(),
                Metadata = string.IsNullOrEmpty(row.Metadata)
                    ? new Dictionary<string, object>()
                    : JsonSerializer.Deserialize<Dictionary<string, object>>(row.Metadata) ?? new Dictionary<string, object>(),
                // These would be loaded separately as needed
                Evidence = new List<Evidence>(),
                Sessions = new List<InvestigationSession>(),
                Timelines = new List<Timeline>(),
                Reports = new List<Report>()
            };
        }

        /// <summary>
        /// Generates a unique case number
        /// </summary>
        private async Task<string> GenerateCaseNumberAsync()
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var count = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM Cases");

            var year = DateTime.UtcNow.Year;
            var month = DateTime.UtcNow.Month;
            return $"IIM-{year:0000}-{month:00}-{count + 1:00000}";
        }

        /// <summary>
        /// Gets the timeline of events for a case.
        /// This is an extension method to avoid breaking existing ICaseManager implementations.
        /// </summary>
        /// <param name="caseManager">The case manager instance</param>
        /// <param name="caseId">ID of the case</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of timeline events</returns>
     
        public async Task<List<TimelineEvent>> GetCaseTimelineAsync(string caseId, CancellationToken cancellationToken = default)
        {
            // Get the case
            var caseEntity = await GetCaseAsync(caseId, cancellationToken);
            if (caseEntity == null)
            {
                return new List<TimelineEvent>();
            }

            var events = new List<TimelineEvent>();

            // Add case creation event


            // Add evidence events if available
            if (caseEntity.Id != null)
            {

            }

            // Add session events if available
            if (caseEntity.Sessions != null)
            {

            }

            // Add case update event
            if (caseEntity.UpdatedAt > caseEntity.CreatedAt)
            {

            }

            // Add case closure event if closed
            if (caseEntity.ClosedAt.HasValue)
            {

            }

            return events;
        }
    }
}