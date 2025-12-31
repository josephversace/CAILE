using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Enums;
using IIM.Shared.Models;
using IIM.Shared.Models.Core;

namespace IIM.Shared.Interfaces
{
	public interface IWorkspaceManager
	{
		// ─────────────────────────────────────────────────────────────
		// WORKSPACE OPERATIONS
		// ─────────────────────────────────────────────────────────────

		Task<Workspace> CreateWorkspaceAsync(string name, string description, WorkspaceType type, CancellationToken cancellationToken = default);
		Task<Workspace?> GetWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default);
		Task<IEnumerable<Workspace>> GetUserWorkspacesAsync(string? userId = null, CancellationToken cancellationToken = default);
		Task<IEnumerable<Workspace>> GetRecentWorkspacesAsync(int count = 10, CancellationToken cancellationToken = default);
		Task<bool> UpdateWorkspaceAsync(Guid workspaceId, Action<Workspace> updateAction, CancellationToken cancellationToken = default);
		Task<bool> DeleteWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default);

		// ─────────────────────────────────────────────────────────────
		// TIMELINE
		// ─────────────────────────────────────────────────────────────

		Task<IEnumerable<TimelineEvent>> GetWorkspaceTimelineAsync(Guid workspaceId, CancellationToken cancellationToken = default);
		Task<TimelineEvent> AddTimelineEventAsync(Guid workspaceId, string eventType, string description, CancellationToken cancellationToken = default);

		// ─────────────────────────────────────────────────────────────
		// WORKSPACE LINKS (SESSIONS + FILES)
		// ─────────────────────────────────────────────────────────────

		Task<bool> LinkSessionToWorkspaceAsync(Guid sessionId, Guid workspaceId, CancellationToken cancellationToken = default);
		Task<bool> UnlinkSessionFromWorkspaceAsync(Guid sessionId, Guid workspaceId, CancellationToken cancellationToken = default);

		Task<bool> LinkFileToWorkspaceAsync(Guid virtualFileId, Guid workspaceId, CancellationToken cancellationToken = default);
		Task<bool> UnlinkFileFromWorkspaceAsync(Guid virtualFileId, Guid workspaceId, CancellationToken cancellationToken = default);

		// ─────────────────────────────────────────────────────────────
		// ARTIFACTS (NEW – CRUD)
		// ─────────────────────────────────────────────────────────────

		/// <summary>Create a new Workspace Artifact.</summary>
		Task<WorkspaceArtifact> CreateArtifactAsync(WorkspaceArtifact artifact, CancellationToken cancellationToken = default);

		/// <summary>Get a single artifact by ID.</summary>
		Task<WorkspaceArtifact?> GetArtifactAsync(Guid artifactId, CancellationToken cancellationToken = default);

		/// <summary>List all artifacts for a given workspace.</summary>
		Task<IEnumerable<WorkspaceArtifact>> GetArtifactsByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default);

		/// <summary>Update an existing artifact.</summary>
		Task<bool> UpdateArtifactAsync(WorkspaceArtifact artifact, CancellationToken cancellationToken = default);

		/// <summary>Soft-delete an artifact.</summary>
		Task<bool> DeleteArtifactAsync(Guid artifactId, CancellationToken cancellationToken = default);

		// Optional convenience:
		/// <summary>Return artifacts that contain a specific tag.</summary>
		Task<IEnumerable<WorkspaceArtifact>> SearchArtifactsByTagAsync(Guid workspaceId, string tag, CancellationToken cancellationToken = default);

		/// <summary>Return artifacts filtered by type (Note, Code, Research, etc.).</summary>
		Task<IEnumerable<WorkspaceArtifact>> GetArtifactsByTypeAsync(Guid workspaceId, ArtifactType type, CancellationToken cancellationToken = default);

		// ─────────────────────────────────────────────────────────────
		// VIRTUAL FILES
		// ─────────────────────────────────────────────────────────────

		Task<VirtualFile> CreateVirtualFileAsync(VirtualFile file, CancellationToken cancellationToken = default);
		Task<VirtualFile?> GetVirtualFileByIdAsync(Guid virtualFileId, CancellationToken cancellationToken = default);
		Task<IEnumerable<VirtualFile>> GetVirtualFilesAsync(Guid workspaceId, CancellationToken cancellationToken = default);
		Task<IEnumerable<VirtualFile>> GetVirtualFilesByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default);
		Task<bool> UpdateVirtualFileAsync(VirtualFile file, CancellationToken cancellationToken = default);

		// ─────────────────────────────────────────────────────────────
		// STORED FILES (DE-DUP STORAGE)
		// ─────────────────────────────────────────────────────────────

		Task<bool> StoredFileExistsAsync(string hash, CancellationToken cancellationToken = default);
		Task<StoredFile?> GetStoredFileByHashAsync(string hash, CancellationToken cancellationToken = default);
		Task<StoredFile> CreateStoredFileAsync(StoredFile storedFile, CancellationToken cancellationToken = default);

		Task<bool> MoveStoredFileAsync(string blake3Hash,string newBucket,CancellationToken ct = default);


		// ─────────────────────────────────────────────────────────────
		// FOLDERS
		// ─────────────────────────────────────────────────────────────

		Task<IEnumerable<object>> GetFolderContentsAsync(Guid workspaceId, string path, CancellationToken cancellationToken = default);


		// ─────────────────────────────────────────────────────────────
		// USERS
		// ─────────────────────────────────────────────────────────────

		Task<bool> AddUserToWorkspaceAsync(Guid workspaceId, string userId, WorkspaceRole role, CancellationToken ct = default);
		Task<bool> RemoveUserFromWorkspaceAsync(Guid workspaceId, string userId, CancellationToken ct = default);
		Task<bool> UpdateWorkspaceUserRoleAsync(Guid workspaceId, string userId, WorkspaceRole role, CancellationToken ct = default);
		// WORKSPACE USERS
		Task<WorkspaceUser?> GetWorkspaceUserAsync(Guid workspaceId, string userId, CancellationToken cancellationToken = default);

		// ─────────────────────────────────────────────────────────────
		// Processed Files
		// ─────────────────────────────────────────────────────────────

		Task<ProcessedFile> AddProcessedFileAsync(ProcessedFile pf, CancellationToken ct = default);
		Task<IEnumerable<ProcessedFile>> GetProcessedFilesAsync(Guid virtualFileId, CancellationToken ct = default);

		Task<List<string>> GetMetadataJsonAsync(string blake3, string processorName, bool latestOnly, CancellationToken ct = default);

		Task<List<string>> GetDerivedHashForProcessedFile(string blake3, string processorName, bool latestOnly, CancellationToken ct = default);

		/// <summary>
		/// Get the derived hash for a processed file.
		/// </summary>
		Task<string?> GetDerivedContentAsync(string storedFileHash, string processorName, CancellationToken ct);

	}
}
