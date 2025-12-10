using IIM.Shared.Models;
using IIM.Shared.Models.Core;
using Microsoft.EntityFrameworkCore;

namespace IIM.Infrastructure.Data
{
	public class WorkspaceDbContext : DbContext
	{
		public WorkspaceDbContext(DbContextOptions<WorkspaceDbContext> options)
			: base(options)
		{
		}

		public DbSet<Workspace> Workspaces => Set<Workspace>();
		public DbSet<WorkspaceUser> WorkspaceUsers => Set<WorkspaceUser>();
		public DbSet<WorkspaceFile> WorkspaceFiles => Set<WorkspaceFile>();
		public DbSet<WorkspaceSession> WorkspaceSessions => Set<WorkspaceSession>();
		public DbSet<WorkspaceArtifact> WorkspaceArtifacts => Set<WorkspaceArtifact>();
		public DbSet<TimelineEvent> TimelineEvents => Set<TimelineEvent>();

		public DbSet<StoredFile> StoredFiles => Set<StoredFile>();
		public DbSet<VirtualFile> VirtualFiles => Set<VirtualFile>();
		public DbSet<ProcessedFile> ProcessedFiles => Set<ProcessedFile>();

		public DbSet<ClassificationTag> ClassificationTags => Set<ClassificationTag>();

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			// StoredFile (Blake3 = PK)
			modelBuilder.Entity<StoredFile>(e =>
			{
				e.HasKey(f => f.Blake3Hash);
				e.Property(f => f.Blake3Hash)
					.HasMaxLength(64);

			
			});

			// VirtualFile
			modelBuilder.Entity<VirtualFile>(e =>
			{
				e.HasKey(v => v.Id);

				e.Property(v => v.CustomMetadataJson)
					.HasColumnType("jsonb");

				e.HasOne(v => v.StoredFile)
					.WithMany(f => f.VirtualFiles)
					.HasForeignKey(v => v.StoredFileHash)
					.HasPrincipalKey(f => f.Blake3Hash)
					.OnDelete(DeleteBehavior.Restrict);
			});

			// ProcessedFile
			// ProcessedFile
			modelBuilder.Entity<ProcessedFile>(e =>
			{
				e.HasKey(p => p.Id);

				e.Property(p => p.MetadataJson)
					.HasColumnType("jsonb");

				// OPTIONAL: a processed file may reference the VirtualFile that triggered processing
				e.HasOne(p => p.VirtualFile)
					.WithMany()
					.HasForeignKey(p => p.VirtualFileId)
					.OnDelete(DeleteBehavior.SetNull);

				// PRIMARY: processed files belong to the StoredFile whose bytes they derive from
				e.HasOne(p => p.StoredFile)
					.WithMany(f => f.ProcessedVersions)
					.HasForeignKey(p => p.StoredFileHash)
					.HasPrincipalKey(s => s.Blake3Hash)
					.OnDelete(DeleteBehavior.Restrict);
			});


			// Classification tags many-to-many
			modelBuilder.Entity<ClassificationTag>()
				.HasMany(t => t.StoredFiles)
				.WithMany(f => f.ClassificationTags);

			// Workspace M:M relations
			modelBuilder.Entity<WorkspaceFile>()
				.HasKey(x => new { x.WorkspaceId, x.VirtualFileId });

			modelBuilder.Entity<WorkspaceSession>()
				.HasKey(x => new { x.WorkspaceId, x.SessionId });

			// WorkspaceUser composite key
			modelBuilder.Entity<WorkspaceUser>()
				.HasKey(x => new { x.WorkspaceId, x.UserId });
		}
	}
}
