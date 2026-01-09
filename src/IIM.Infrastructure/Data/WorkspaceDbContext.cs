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

		public DbSet<IngestionStepState> IngestionStepStates => Set<IngestionStepState>();


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
					.IsRequired(true)
					.OnDelete(DeleteBehavior.Restrict);
			});

			// ProcessedFile
			modelBuilder.Entity<ProcessedFile>(e =>
			{
				e.HasKey(p => p.Id);

				// ---- Required scalar fields ----
				e.Property(p => p.StoredFileHash)
					.IsRequired();

				e.Property(p => p.DerivedHash)
					.IsRequired();

				e.Property(p => p.ProcessorName)
					.IsRequired();

				e.Property(p => p.ProcessorKind)
					.IsRequired();

				e.Property(p => p.ProcessedAt)
					.IsRequired();

				// ---- JSON metadata ----
				e.Property(p => p.MetadataJson)
					.HasColumnType("jsonb")
					.IsRequired();

				// ---- Relationship: StoredFile (content-addressed) ----
				e.HasOne(p => p.StoredFile)
					.WithMany(f => f.ProcessedVersions)
					.HasForeignKey(p => p.StoredFileHash)
					.HasPrincipalKey(s => s.Blake3Hash)
					.OnDelete(DeleteBehavior.Restrict);

				// ---- Deduplication / identity constraint ----
				e.HasIndex(p => new
				{
					p.StoredFileHash,
					p.ProcessorName,
					p.ProcessorVersion,
					p.ParametersHash
				})
					.IsUnique();

				// ---- Optional: fast lookup by derived output ----
				e.HasIndex(p => p.DerivedHash);
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

			// IngestionStepState (Step ledger)
			modelBuilder.Entity<IngestionStepState>(e =>
			{
				e.HasKey(x => x.Id);

				e.Property(x => x.StoredFileHash).IsRequired();
				e.Property(x => x.StepId).IsRequired();
				e.Property(x => x.StepVersion).IsRequired();
				e.Property(x => x.InputHash).IsRequired();

				// Keep these reasonably bounded (optional but helps)
				e.Property(x => x.StoredFileHash).HasMaxLength(128);
				e.Property(x => x.StepId).HasMaxLength(128);
				e.Property(x => x.StepVersion).HasMaxLength(64);
				e.Property(x => x.InputHash).HasMaxLength(128);
				e.Property(x => x.OutputHash).HasMaxLength(128);
				e.Property(x => x.ParametersHash).HasMaxLength(128);

				e.Property(x => x.MetadataJson).HasColumnType("jsonb");
				e.Property(x => x.LastError);

				// Identity/dedup constraint (this is your resumability key)
				e.HasIndex(x => new
				{
					x.StoredFileHash,
					x.StepId,
					x.StepVersion,
					x.InputHash,
					x.ParametersHash
				}).IsUnique();

				// Query accelerators
				e.HasIndex(x => new { x.StoredFileHash, x.Status });
				e.HasIndex(x => new { x.WorkspaceId, x.VirtualFileId });
				e.HasIndex(x => x.UpdatedAt);
			});

		}
	}
}
