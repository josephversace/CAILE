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
    /// SQLite database implementation of IWorkspaceManager
    /// </summary>
    public class SqliteCaseManager : IWorkspaceManager
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
        public async Task<Workspace> CreateWorkspaceAsync(string name, string description, WorkspaceType type,
            CancellationToken cancellationToken = default)
        {
            var caseEntity = new Workspace
            {
                Id = Guid.NewGuid().ToString("N"),
                CaseNumber = await GenerateWorkspaceNumberAsync(),
                Title = name,
                Description = description,
                Type = type,
                Status = WorkspaceStatus.Open,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                Priority = WorkspacePriority.Medium,
                Owner = Environment.UserName,
                TeamMembers = new List<string> { Environment.UserName },
                Files = new List<ManagedFile>(),
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
                INSERT INTO Workspaces (
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
                caseEntity.Title,
                Type = type.ToString(),
                Status = caseEntity.Status.ToString(),
                caseEntity.Description,
                caseEntity.Owner,
                TeamMembersJson = JsonSerializer.Serialize(caseEntity.TeamMembers),
                caseEntity.CreatedAt,
                caseEntity.UpdatedAt,
                Priority = caseEntity.Priority.ToString(),
                caseEntity.Classification,
                AccessControlListJson = JsonSerializer.Serialize(caseEntity.AccessControlList),
                MetadataJson = JsonSerializer.Serialize(caseEntity.Metadata)
            });

            _logger.LogInformation("Created workspace {CaseId} with number {CaseNumber}",
                caseEntity.Id, caseEntity.CaseNumber);

            return caseEntity;
        }

        /// <summary>
        /// Retrieves a case from the SQLite database
        /// </summary>
        public async Task<Workspace?> GetWorkspaceAsync(string workspaceId, CancellationToken cancellationToken = default)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string sql = @"
                SELECT * FROM Workspaces WHERE Id = @WorkspaceId AND IsDeleted = 0";

            var row = await connection.QueryFirstOrDefaultAsync(sql, new { WorkspaceId = workspaceId });

            if (row == null)
            {
                return null;
            }

            return MapRowToWorkspace(row);
        }

        /// <summary>
        /// Retrieves all cases from the SQLite database
        /// </summary>
        public async Task<List<Workspace>> GetUserWorkspacesAsync(string? userId = null,
            CancellationToken cancellationToken = default)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            string sql = @"
                SELECT * FROM Workspaces 
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

            return rows.Select(MapRowToWorkspace).ToList();
        }

        /// <summary>
        /// Updates a case in the SQLite database
        /// </summary>
        public async Task<bool> UpdateWorkspaceAsync(string workspaceId, Action<Workspace> updateAction,
            CancellationToken cancellationToken = default)
        {
            var workspaceEntity = await GetWorkspaceAsync(workspaceId, cancellationToken);
            if (workspaceEntity == null)
            {
                return false;
            }

            updateAction(workspaceEntity);
            workspaceEntity.UpdatedAt = DateTimeOffset.UtcNow;

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string sql = @"
                UPDATE Workspaces SET
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
                workspaceEntity.Id,
                workspaceEntity.Title,
                workspaceEntity.Description,
                Status = workspaceEntity.Status.ToString(),
                workspaceEntity.UpdatedAt,
                Priority = workspaceEntity.Priority.ToString(),
                workspaceEntity.Owner,
                TeamMembersJson = JsonSerializer.Serialize(workspaceEntity.TeamMembers),
                workspaceEntity.Classification,
                AccessControlListJson = JsonSerializer.Serialize(workspaceEntity.AccessControlList),
                MetadataJson = JsonSerializer.Serialize(workspaceEntity.Metadata)
            });

            return rowsAffected > 0;
        }

        /// <summary>
        /// Links a session to a case
        /// </summary>
        public async Task<bool> LinkSessionToWorkspaceAsync(string sessionId, string workspaceId,
            CancellationToken cancellationToken = default)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string sql = @"
                INSERT INTO WorkspaceSessions (WorkspaceId, SessionId, LinkedAt)
                VALUES (@WorkspaceId, @SessionId, @LinkedAt)";

            var rowsAffected = await connection.ExecuteAsync(sql, new
            {
                WorkspaceId = workspaceId,
                SessionId = sessionId,
                LinkedAt = DateTimeOffset.UtcNow
            });

            return rowsAffected > 0;
        }

        /// <summary>
        /// Links evidence to a case
        /// </summary>
        public async Task<bool> LinkFileToWorkspaceAsync(string fileId, string workspaceId,
            CancellationToken cancellationToken = default)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string sql = @"
                INSERT INTO WorkspaceFile (WorkspaceId, FileId, LinkedAt)
                VALUES (@WorkspaceId, @FileId, @LinkedAt)";

            var rowsAffected = await connection.ExecuteAsync(sql, new
            {
                WorkspaceId = workspaceId,
                FileId = workspaceId,
                LinkedAt = DateTimeOffset.UtcNow
            });

            return rowsAffected > 0;
        }

        /// <summary>
        /// Gets recent cases
        /// </summary>
        public async Task<List<Workspace>> GetRecentWorkspacesAsync(int count = 10,
            CancellationToken cancellationToken = default)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string sql = @"
                SELECT * FROM Workspaces
                WHERE IsDeleted = 0
                ORDER BY UpdatedAt DESC
                LIMIT @Count";

            var rows = await connection.QueryAsync(sql, new { Count = count });
            return rows.Select(MapRowToWorkspace).ToList();
        }

        /// <summary>
        /// Soft deletes a case
        /// </summary>
        public async Task<bool> DeleteWorkspaceAsync(string workspaceId, CancellationToken cancellationToken = default)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string sql = @"
                UPDATE Workspaces 
                SET IsDeleted = 1, UpdatedAt = @UpdatedAt
                WHERE Id = @WorkspaceId";

            var rowsAffected = await connection.ExecuteAsync(sql, new
            {
                WorkspaceId = workspaceId,
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
                CREATE TABLE IF NOT EXISTS Workspaces (
                    Id TEXT PRIMARY KEY,
                    WorkspaceNumber TEXT NOT NULL UNIQUE,
                    Name TEXT NOT NULL,
                    Type TEXT NOT NULL,
                    Status TEXT NOT NULL,
                    Description TEXT,
                    Owner TEXT,
                    TeamMembers TEXT,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL,
                    Priority TEXT,
                    Classification TEXT,
                    AccessControlList TEXT,
                    Metadata TEXT,
                    IsDeleted INTEGER DEFAULT 0
                );

                CREATE INDEX IF NOT EXISTS idx_workspaces_status ON Workspaces(Status);
                CREATE INDEX IF NOT EXISTS idx_workspaces_updated ON Workspaces(UpdatedAt);
                CREATE INDEX IF NOT EXISTS idx_workspaces_owner ON Workspaces(Owner);";

            await connection.ExecuteAsync(createCasesTable);

            const string createLinkTables = @"
                CREATE TABLE IF NOT EXISTS WorkspaceSessions (
                    WorkspaceId TEXT NOT NULL,
                    SessionId TEXT NOT NULL,
                    LinkedAt TEXT NOT NULL,
                    PRIMARY KEY (CaseId, SessionId),
                    FOREIGN KEY (CaseId) REFERENCES Workspaces(Id)
                );

                CREATE TABLE IF NOT EXISTS WorkspaceFiles (
                    WorkspaceId TEXT NOT NULL,
                    FileId TEXT NOT NULL,
                    LinkedAt TEXT NOT NULL,
                    PRIMARY KEY (WorkspaceId, FileId),
                    FOREIGN KEY (WorkspaceId) REFERENCES Workspaces(Id)
                );";

            await connection.ExecuteAsync(createLinkTables);

            _logger.LogInformation("SQLite database initialized at {Path}", _config.SqlitePath);
        }

        /// <summary>
        /// Maps a database row to a workspace object
        /// </summary>
        private Workspace MapRowToWorkspace(dynamic row)
        {
            return new Workspace
            {
                Id = row.Id,
                CaseNumber = row.CaseNumber,
                Title = row.Name,
                Type = Enum.Parse<WorkspaceType>(row.Type),
                Status = Enum.Parse<WorkspaceStatus>(row.Status),
                Description = row.Description,
                Owner = row.LeadInvestigator,
                TeamMembers = string.IsNullOrEmpty(row.TeamMembers)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(row.TeamMembers) ?? new List<string>(),
                CreatedAt = DateTimeOffset.Parse(row.CreatedAt),
                UpdatedAt = DateTimeOffset.Parse(row.UpdatedAt),
                Priority = Enum.Parse<WorkspacePriority>(row.Priority),
                Classification = row.Classification,
                AccessControlList = string.IsNullOrEmpty(row.AccessControlList)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(row.AccessControlList) ?? new List<string>(),
                Metadata = string.IsNullOrEmpty(row.Metadata)
                    ? new Dictionary<string, object>()
                    : JsonSerializer.Deserialize<Dictionary<string, object>>(row.Metadata) ?? new Dictionary<string, object>(),
                // These would be loaded separately as needed
                Files = new List<ManagedFile>(),
                Sessions = new List<InvestigationSession>(),
                Timelines = new List<Timeline>(),
                Reports = new List<Report>()
            };
        }

        /// <summary>
        /// Generates a unique workspace number
        /// </summary>
        private async Task<string> GenerateWorkspaceNumberAsync()
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var count = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM Workspaces");

            var year = DateTime.UtcNow.Year;
            var month = DateTime.UtcNow.Month;
            return $"IIM-{year:0000}-{month:00}-{count + 1:00000}";
        }

        /// <summary>
        /// Gets the timeline of events for a case.
        /// This is an extension method to avoid breaking existing IWorkspaceManager implementations.
        /// </summary>
        /// <param name="workspaceManager">The workspace manager instance</param>
        /// <param name="workspaceId">ID of the workspace</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of timeline events</returns>
     
        public async Task<List<TimelineEvent>> GetWorkspaceTimelineAsync(string workspaceId, CancellationToken cancellationToken = default)
        {
            // Get the case
            var workspaceEntity = await GetWorkspaceAsync(workspaceId, cancellationToken);
            if (workspaceEntity == null)
            {
                return new List<TimelineEvent>();
            }

            var events = new List<TimelineEvent>();

            // Add case creation event


            // Add evidence events if available
            if (workspaceEntity.Id != null)
            {

            }

            // Add session events if available
            if (workspaceEntity.Sessions != null)
            {

            }

            // Add case update event
            if (workspaceEntity.UpdatedAt > workspaceEntity.CreatedAt)
            {

            }

            // Add case closure event if closed
            if (workspaceEntity.ClosedAt.HasValue)
            {

            }

            return events;
        }
    }
}