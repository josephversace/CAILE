using System.Linq.Expressions;
using IIM.Shared.Enums;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using IIM.Shared.Models.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IIM.Infrastructure.Data
{
	public class EfWorkspaceManager : IWorkspaceManager
	{
		private readonly WorkspaceDbContext _db;
		private readonly ILogger<EfWorkspaceManager> _logger;
		private readonly IUserRepository _users;
		private readonly IFileStore _fileStore;

		public EfWorkspaceManager(
			WorkspaceDbContext db,
			ILogger<EfWorkspaceManager> logger,
			IUserRepository users, IFileStore fileStore)
		{
			_db = db;
			_logger = logger;
			_users = users;
			_fileStore = fileStore;
		}


		// ============================================================
		// CREATE WORKSPACE
		// ============================================================

		public async Task<Workspace> CreateWorkspaceAsync(
			string name,
			string description,
			WorkspaceType type,
			CancellationToken cancellationToken = default)
		{
			var ws = new Workspace
			{
				Id = Guid.NewGuid(),
				Name = name,
				Description = description,
				Type = type,
				CreatedAt = DateTimeOffset.UtcNow,
				UpdatedAt = DateTimeOffset.UtcNow,
				IsDeleted = false
			};

			_db.Workspaces.Add(ws);
			await _db.SaveChangesAsync(cancellationToken);

			return ws;
		}

		// ============================================================
		// GET SINGLE WORKSPACE
		// ============================================================

		public async Task<Workspace?> GetWorkspaceAsync(
			Guid workspaceId,
			CancellationToken cancellationToken = default)
		{
			var ws = await _db.Workspaces
				.Include(w => w.Files)
				.Include(w => w.Sessions)
				.Include(w => w.Users)
				.FirstOrDefaultAsync(w => w.Id == workspaceId, cancellationToken);

			if (ws == null)
				return null;

			// Hydrate Identity info
			foreach (var wu in ws.Users)
			{
				var appUser = await _users.GetByIdAsync(wu.UserId);
				if (appUser != null)
				{
					wu.DisplayName = appUser.UserName ?? appUser.Email!;
					wu.Email = appUser.Email!;
					wu.User = appUser;
				}
			}

			return ws;
		}


		// ============================================================
		// GET USER WORKSPACES
		// ============================================================

		public async Task<IEnumerable<Workspace>> GetUserWorkspacesAsync(
			string? userId = null,
			CancellationToken cancellationToken = default)
		{
			var query = _db.Workspaces.Where(w => !w.IsDeleted).AsQueryable();

			if (!string.IsNullOrEmpty(userId))
				query = query.Where(w => w.Users.Any(u => u.UserId == userId));

			return await query
				.OrderByDescending(w => w.UpdatedAt)
				.ToListAsync(cancellationToken);
		}


		// ============================================================
		// UPDATE WORKSPACE
		// ============================================================

		public async Task<bool> UpdateWorkspaceAsync(
			Guid workspaceId,
			Action<Workspace> updateAction,
			CancellationToken cancellationToken = default)
		{
			var ws = await _db.Workspaces
				.FirstOrDefaultAsync(w => w.Id == workspaceId, cancellationToken);

			if (ws == null)
				return false;

			updateAction(ws);
			ws.UpdatedAt = DateTimeOffset.UtcNow;

			await _db.SaveChangesAsync(cancellationToken);
			return true;
		}


		// ============================================================
		// LINK SESSION
		// ============================================================

		public async Task<bool> LinkSessionToWorkspaceAsync(
			Guid sessionId,
			Guid workspaceId,
			CancellationToken cancellationToken = default)
		{
			bool exists = await _db.WorkspaceSessions.AnyAsync(
				x => x.WorkspaceId == workspaceId && x.SessionId == sessionId,
				cancellationToken);

			if (exists)
				return true;

			var link = new WorkspaceSession
			{
				WorkspaceId = workspaceId,
				SessionId = sessionId
			};

			_db.WorkspaceSessions.Add(link);
			await _db.SaveChangesAsync(cancellationToken);

			return true;
		}


		public async Task<bool> UnlinkSessionFromWorkspaceAsync(
			Guid sessionId,
			Guid workspaceId,
			CancellationToken cancellationToken = default)
		{
			var link = await _db.WorkspaceSessions
				.FirstOrDefaultAsync(
					x => x.WorkspaceId == workspaceId && x.SessionId == sessionId,
					cancellationToken);

			if (link == null)
				return false;

			_db.WorkspaceSessions.Remove(link);
			await _db.SaveChangesAsync(cancellationToken);

			return true;
		}


		// ============================================================
		// LINK FILE
		// ============================================================

		public async Task<bool> LinkFileToWorkspaceAsync(
			Guid virtualFileId,
			Guid workspaceId,
			CancellationToken cancellationToken = default)
		{
			bool exists = await _db.WorkspaceFiles.AnyAsync(
				x => x.WorkspaceId == workspaceId && x.VirtualFileId == virtualFileId,
				cancellationToken);

			if (exists)
				return true;

			var link = new WorkspaceFile
			{
				WorkspaceId = workspaceId,
				VirtualFileId = virtualFileId
			};

			_db.WorkspaceFiles.Add(link);
			await _db.SaveChangesAsync(cancellationToken);

			return true;
		}


		public async Task<bool> UnlinkFileFromWorkspaceAsync(
			Guid virtualFileId,
			Guid workspaceId,
			CancellationToken cancellationToken = default)
		{
			var link = await _db.WorkspaceFiles
				.FirstOrDefaultAsync(
					x => x.WorkspaceId == workspaceId && x.VirtualFileId == virtualFileId,
					cancellationToken);

			if (link == null)
				return false;

			_db.WorkspaceFiles.Remove(link);
			await _db.SaveChangesAsync(cancellationToken);

			return true;
		}


		// ============================================================
		// RECENT WORKSPACES
		// ============================================================

		public async Task<IEnumerable<Workspace>> GetRecentWorkspacesAsync(
			int count = 10,
			CancellationToken cancellationToken = default)
		{
			return await _db.Workspaces
				.Where(w => !w.IsDeleted)
				.OrderByDescending(w => w.UpdatedAt)
				.Take(count)
				.ToListAsync(cancellationToken);
		}


		// ============================================================
		// DELETE WORKSPACE (SOFT DELETE)
		// ============================================================

		public async Task<bool> DeleteWorkspaceAsync(
			Guid workspaceId,
			CancellationToken cancellationToken = default)
		{
			var ws = await _db.Workspaces
				.FirstOrDefaultAsync(w => w.Id == workspaceId, cancellationToken);

			if (ws == null)
				return false;

			ws.IsDeleted = true;
			ws.UpdatedAt = DateTimeOffset.UtcNow;

			await _db.SaveChangesAsync(cancellationToken);
			return true;
		}


		// ============================================================
		// TIMELINE
		// ============================================================

		public async Task<IEnumerable<TimelineEvent>> GetWorkspaceTimelineAsync(
			Guid workspaceId,
			CancellationToken cancellationToken = default)
		{
			return await _db.TimelineEvents
				.Where(t => t.WorkspaceId == workspaceId)
				.OrderByDescending(t => t.Timestamp)
				.ToListAsync(cancellationToken);
		}


		public async Task<TimelineEvent> AddTimelineEventAsync(
			Guid workspaceId,
			string eventType,
			string description,
			CancellationToken cancellationToken = default)
		{
			var ev = new TimelineEvent
			{
				Id = Guid.NewGuid(),
				WorkspaceId = workspaceId,
				EventType = eventType,
				Description = description,
				Timestamp = DateTimeOffset.UtcNow
			};

			_db.TimelineEvents.Add(ev);
			await _db.SaveChangesAsync(cancellationToken);
			return ev;
		}


		// ============================================================
		// VIRTUAL FILES
		// ============================================================

		public async Task<VirtualFile> CreateVirtualFileAsync(
			VirtualFile file,
			CancellationToken cancellationToken = default)
		{
			if (file.Id == Guid.Empty)
				file.Id = Guid.NewGuid();

			_db.VirtualFiles.Add(file);
			await _db.SaveChangesAsync(cancellationToken);
			return file;
		}


		public async Task<VirtualFile?> GetVirtualFileByIdAsync(
	Guid virtualFileId,
	CancellationToken cancellationToken = default)
		{
			return await _db.VirtualFiles
				.Include(v => v.ChainOfCustody)
				.Include(v => v.StoredFile).ThenInclude(sf => sf.ProcessedVersions)
				.FirstOrDefaultAsync(v => v.Id == virtualFileId, cancellationToken);
		}



		public async Task<IEnumerable<VirtualFile>> GetVirtualFilesAsync(
			Guid workspaceId,
			CancellationToken cancellationToken = default)
		{
			return await _db.VirtualFiles
				.Where(v => v.WorkspaceId == workspaceId)
				.ToListAsync(cancellationToken);
		}


		public async Task<IEnumerable<VirtualFile>> GetVirtualFilesByWorkspaceAsync(
			Guid workspaceId,
			CancellationToken cancellationToken = default)
		{
			return await GetVirtualFilesAsync(workspaceId, cancellationToken);
		}


		public async Task<bool> UpdateVirtualFileAsync(
			VirtualFile file,
			CancellationToken cancellationToken = default)
		{
			_db.VirtualFiles.Update(file);
			await _db.SaveChangesAsync(cancellationToken);
			return true;
		}


		// ============================================================
		// STORED FILES (PHYSICAL)
		// ============================================================

		public async Task<bool> StoredFileExistsAsync(
			string blake3Hash,
			CancellationToken cancellationToken = default)
		{
			return await _db.StoredFiles
				.AnyAsync(sf => sf.Blake3Hash == blake3Hash, cancellationToken);
		}


		public async Task<StoredFile?> GetStoredFileByHashAsync(
			string blake3Hash,
			CancellationToken cancellationToken = default)
		{
			return await _db.StoredFiles
				.Include(sf => sf.ClassificationTags)
				.FirstOrDefaultAsync(sf => sf.Blake3Hash == blake3Hash, cancellationToken);
		}


		public async Task<StoredFile> CreateStoredFileAsync(
			StoredFile file,
			CancellationToken cancellationToken = default)
		{
			_db.StoredFiles.Add(file);
			await _db.SaveChangesAsync(cancellationToken);
			return file;
		}

		public async Task<bool> MoveStoredFileAsync(
		string blake3Hash,
		string newBucket,
		CancellationToken ct = default)
		{
			var stored = await _db.StoredFiles
				.FirstOrDefaultAsync(s => s.Blake3Hash == blake3Hash, ct);

			if (stored == null)
				return false;

			var oldBucket = stored.Bucket;
			var objectKey = stored.StoragePath;

			if (oldBucket == newBucket)
				return true;

			// Atomic server-side move between collections
			await _fileStore.PromoteAsync(oldBucket, newBucket, objectKey, ct);

			// Update metadata (only bucket changes, objectKey stays same)
			stored.Bucket = newBucket;

			await _db.SaveChangesAsync(ct);

			// TODO: chain-of-custody + timeline event

			return true;
		}



		// ============================================================
		// FOLDER CONTENTS
		// ============================================================

		public async Task<IEnumerable<object>> GetFolderContentsAsync(
			Guid workspaceId,
			string path,
			CancellationToken cancellationToken = default)
		{
			var files = await _db.VirtualFiles
				.Where(v => v.WorkspaceId == workspaceId && v.Path == path)
				.ToListAsync(cancellationToken);

			return files.Cast<object>().ToList();
		}


		// ============================================================
		// ARTIFACTS (NOTES, CODE, PLANS, RESEARCH)
		// ============================================================

		public async Task<WorkspaceArtifact> CreateArtifactAsync(
			WorkspaceArtifact artifact,
			CancellationToken cancellationToken = default)
		{
			if (artifact.Id == Guid.Empty)
				artifact.Id = Guid.NewGuid();

			artifact.CreatedUtc = DateTime.UtcNow;
			artifact.UpdatedUtc = artifact.CreatedUtc;

			_db.WorkspaceArtifacts.Add(artifact);
			await _db.SaveChangesAsync(cancellationToken);

			try
			{
				await AddTimelineEventAsync(
					artifact.WorkspaceId,
					"artifact.created",
					$"Artifact created: {artifact.Title}",
					cancellationToken);
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Timeline event failed for artifact creation");
			}

			return artifact;
		}


		public async Task<WorkspaceArtifact?> GetArtifactAsync(
			Guid artifactId,
			CancellationToken cancellationToken = default)
		{
			return await _db.WorkspaceArtifacts
				.FirstOrDefaultAsync(a => a.Id == artifactId && !a.IsDeleted, cancellationToken);
		}


		public async Task<IEnumerable<WorkspaceArtifact>> GetArtifactsByWorkspaceAsync(
			Guid workspaceId,
			CancellationToken cancellationToken = default)
		{
			return await _db.WorkspaceArtifacts
				.Where(a => a.WorkspaceId == workspaceId && !a.IsDeleted)
				.OrderByDescending(a => a.CreatedUtc)
				.ToListAsync(cancellationToken);
		}


		public async Task<bool> UpdateArtifactAsync(
			WorkspaceArtifact artifact,
			CancellationToken cancellationToken = default)
		{
			var existing = await _db.WorkspaceArtifacts
				.FirstOrDefaultAsync(a => a.Id == artifact.Id && !a.IsDeleted, cancellationToken);

			if (existing == null)
				return false;

			existing.Title = artifact.Title;
			existing.Summary = artifact.Summary;
			existing.Content = artifact.Content;
			existing.Tags = artifact.Tags;
			existing.UpdatedUtc = DateTime.UtcNow;

			await _db.SaveChangesAsync(cancellationToken);

			try
			{
				await AddTimelineEventAsync(
					existing.WorkspaceId,
					"artifact.updated",
					$"Artifact updated: {existing.Title}",
					cancellationToken);
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Timeline logging failed");
			}

			return true;
		}


		public async Task<bool> DeleteArtifactAsync(
			Guid artifactId,
			CancellationToken cancellationToken = default)
		{
			var artifact = await _db.WorkspaceArtifacts
				.FirstOrDefaultAsync(a => a.Id == artifactId && !a.IsDeleted, cancellationToken);

			if (artifact == null)
				return false;

			artifact.IsDeleted = true;
			artifact.UpdatedUtc = DateTime.UtcNow;

			await _db.SaveChangesAsync(cancellationToken);

			try
			{
				await AddTimelineEventAsync(
					artifact.WorkspaceId,
					"artifact.deleted",
					$"Artifact deleted: {artifact.Title}",
					cancellationToken);
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Timeline logging failed");
			}

			return true;
		}


		public async Task<IEnumerable<WorkspaceArtifact>> SearchArtifactsByTagAsync(
			Guid workspaceId,
			string tag,
			CancellationToken cancellationToken = default)
		{
			tag = tag.ToLowerInvariant();

			return await _db.WorkspaceArtifacts
				.Where(a =>
					a.WorkspaceId == workspaceId &&
					!a.IsDeleted &&
					a.Tags.Any(t => t.ToLower() == tag))
				.OrderByDescending(a => a.CreatedUtc)
				.ToListAsync(cancellationToken);
		}


		public async Task<IEnumerable<WorkspaceArtifact>> GetArtifactsByTypeAsync(
			Guid workspaceId,
			ArtifactType type,
			CancellationToken cancellationToken = default)
		{
			return await _db.WorkspaceArtifacts
				.Where(a =>
					a.WorkspaceId == workspaceId &&
					a.Type == type &&
					!a.IsDeleted)
				.OrderByDescending(a => a.CreatedUtc)
				.ToListAsync(cancellationToken);
		}


		// ============================================================
		// USER WORKSPACES + ROLES
		// ============================================================

		public async Task<bool> AddUserToWorkspaceAsync(
			Guid workspaceId,
			string userId,
			WorkspaceRole role,
			CancellationToken ct = default)
		{
			var ws = await _db.Workspaces
				.Include(w => w.Users)
				.FirstOrDefaultAsync(w => w.Id == workspaceId, ct);

			if (ws == null)
				return false;

			if (ws.Users.Any(u => u.UserId == userId))
				return true;

			var appUser = await _users.GetByIdAsync(userId);

			var wu = new WorkspaceUser
			{
				WorkspaceId = workspaceId,
				UserId = userId,
				Role = role,
				DisplayName = appUser?.UserName ?? appUser?.Email ?? "",
				Email = appUser?.Email ?? "",
				AddedAt = DateTimeOffset.UtcNow,
				User = appUser
			};

			ws.Users.Add(wu);
			ws.UpdatedAt = DateTimeOffset.UtcNow;

			await _db.SaveChangesAsync(ct);
			return true;
		}


		public async Task<bool> UpdateWorkspaceUserRoleAsync(
			Guid workspaceId,
			string userId,
			WorkspaceRole role,
			CancellationToken ct = default)
		{
			var wu = await _db.WorkspaceUsers
				.FirstOrDefaultAsync(u => u.WorkspaceId == workspaceId && u.UserId == userId, ct);

			if (wu == null)
				return false;

			wu.Role = role;

			await _db.SaveChangesAsync(ct);
			return true;
		}


		public async Task<bool> RemoveUserFromWorkspaceAsync(
			Guid workspaceId,
			string userId,
			CancellationToken ct = default)
		{
			var wu = await _db.WorkspaceUsers
				.FirstOrDefaultAsync(u => u.WorkspaceId == workspaceId && u.UserId == userId, ct);

			if (wu == null)
				return false;

			_db.WorkspaceUsers.Remove(wu);

			var ws = await _db.Workspaces.FindAsync(workspaceId);
			if (ws != null)
				ws.UpdatedAt = DateTimeOffset.UtcNow;

			await _db.SaveChangesAsync(ct);
			return true;
		}


		public async Task<WorkspaceUser?> GetWorkspaceUserAsync(Guid workspaceId,	string userId, CancellationToken cancellationToken = default)
		{
			var wu = await _db.WorkspaceUsers
				.AsNoTracking()
				.FirstOrDefaultAsync(
					x => x.WorkspaceId == workspaceId && x.UserId == userId,
					cancellationToken);

			if (wu == null)
				return null;

			var appUser = await _users.GetByIdAsync(userId);

			if (appUser != null)
			{
				wu.DisplayName = appUser.UserName ?? appUser.Email!;
				wu.Email = appUser.Email!;
				wu.User = appUser;
			}

			return wu;
		}


		// ============================================================
		// Processed Files
		// ============================================================
		public async Task<ProcessedFile> AddProcessedFileAsync(ProcessedFile pf,CancellationToken ct = default)
		{
			_db.ProcessedFiles.Add(pf);
			await _db.SaveChangesAsync(ct);
			return pf;
		}

		public async Task<IEnumerable<ProcessedFile>> GetProcessedFilesAsync(
			Guid virtualFileId,
			CancellationToken ct = default)
		{
			var processedVersions = await (from v in _db.VirtualFiles join p in _db.ProcessedFiles	on v.StoredFileHash equals p.StoredFileHash where v.Id == virtualFileId	select p).ToListAsync();

			return processedVersions;
		}

		public async Task<List<string>> GetMetadataJsonAsync(string blake3,string processorName, bool latestOnly, CancellationToken ct = default)
		{
			var query = _db.ProcessedFiles
				.Where(pf =>
					pf.StoredFileHash == blake3 &&
					pf.ProcessorName == processorName);

			if (latestOnly)
			{
				return await query
					.OrderByDescending(pf => pf.ProcessedAt)
					.Select(pf => pf.MetadataJson)
					.Take(1)
					.ToListAsync(ct);
			}

			return await query
				.OrderBy(pf => pf.ProcessedAt)
				.Select(pf => pf.MetadataJson)
				.ToListAsync(ct);
		}

		public async Task<List<string>> GetDerivedHashForProcessedFile(string blake3, string processorName, bool latestOnly, CancellationToken ct = default)
		{
			var query = _db.ProcessedFiles
				.Where(pf =>
					pf.StoredFileHash == blake3 &&
					pf.ProcessorName == processorName);

			if (latestOnly)
			{
				return await query
					.OrderByDescending(pf => pf.ProcessedAt)
					.Select(pf => pf.DerivedHash)
					.Take(1)
					.ToListAsync(ct);
			}

			return await query
				.OrderBy(pf => pf.ProcessedAt)
				.Select(pf => pf.DerivedHash)
				.ToListAsync(ct);
		}

        public async Task<string?> GetDerivedHashAsync(string storedFileHash, string processorName, CancellationToken ct)
        {
			var query = _db.ProcessedFiles
				.Where(pf =>
					pf.StoredFileHash == storedFileHash &&
					pf.ProcessorName == processorName);

			return await query
					 .Select(pf => pf.DerivedHash).SingleOrDefaultAsync(ct);

		}
    }
}
