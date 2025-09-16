using IIM.Shared.Models;
using IIM.Shared.Models.Core;
using Microsoft.EntityFrameworkCore;

namespace IIM.Infrastructure.Data;

public class FileDbContext : DbContext
{
    public FileDbContext(DbContextOptions<FileDbContext> options) : base(options) { }

    // DbSets for the corrected architecture
    public DbSet<StoredFile> StoredFiles { get; set; }
    public DbSet<VirtualFile> VirtualFiles { get; set; }
    public DbSet<VirtualFolder> VirtualFolders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure StoredFile to use Hash as primary key
        modelBuilder.Entity<StoredFile>()
            .HasKey(sf => sf.Hash);

        // Configure VirtualFile
        modelBuilder.Entity<VirtualFile>()
            .HasKey(vf => vf.Id);

        // Add index on StoredFileHash for performance (foreign key reference)
        modelBuilder.Entity<VirtualFile>()
            .HasIndex(vf => vf.StoredFileHash)
            .HasDatabaseName("IX_VirtualFile_StoredFileHash");

        // Add index on WorkspaceId for performance
        modelBuilder.Entity<VirtualFile>()
            .HasIndex(vf => vf.WorkspaceId)
            .HasDatabaseName("IX_VirtualFile_WorkspaceId");

        // Add composite index for workspace + path queries
        modelBuilder.Entity<VirtualFile>()
            .HasIndex(vf => new { vf.WorkspaceId, vf.Path })
            .HasDatabaseName("IX_VirtualFile_WorkspaceId_Path");

        // Configure VirtualFolder (if you have this entity)
        modelBuilder.Entity<VirtualFolder>()
            .HasKey(vf => new { vf.Name, vf.Path }); // Composite key

        // Configure ClassificationTags many-to-many relationship with StoredFile
        modelBuilder.Entity<StoredFile>()
            .HasMany(sf => sf.ClassificationTags)
            .WithMany()
            .UsingEntity(j => j.ToTable("StoredFileClassificationTags"));

        // Configure string length constraints
        modelBuilder.Entity<StoredFile>(entity =>
        {
            entity.Property(e => e.Hash)
                .HasMaxLength(64) // SHA256 is 64 chars
                .IsRequired();

            entity.Property(e => e.MimeType)
                .HasMaxLength(255);
        });

        modelBuilder.Entity<VirtualFile>(entity =>
        {
            entity.Property(e => e.FileName)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.Path)
                .HasMaxLength(2048)
                .IsRequired();

            entity.Property(e => e.StoredFileHash)
                .HasMaxLength(64); // SHA256 is 64 chars

            entity.Property(e => e.CreatedBy)
                .HasMaxLength(255);

            entity.Property(e => e.CollectedBy)
                .HasMaxLength(255);

            entity.Property(e => e.CollectedLocation)
                .HasMaxLength(500);
        });

        modelBuilder.Entity<VirtualFolder>(entity =>
        {
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.Path)
                .HasMaxLength(2048)
                .IsRequired();
        });
    }
}