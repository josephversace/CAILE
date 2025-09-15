using IIM.Shared.Models.Core;
using Microsoft.EntityFrameworkCore;

namespace IIM.Infrastructure.Data;

public class FileDbContext : DbContext
{
    public FileDbContext(DbContextOptions<FileDbContext> options) : base(options) { }

    // Remove the old DbSet<ManagedFile>
    // public DbSet<ManagedFile> Files { get; set; }

    // Add the new DbSets for our corrected architecture
    public DbSet<StoredFile> StoredFiles { get; set; }
    public DbSet<VirtualFile> VirtualFiles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure the relationship between VirtualFile and StoredFile
        modelBuilder.Entity<VirtualFile>()
            .HasOne(vf => vf.StoredFile)
            .WithMany(sf => sf.VirtualFiles)
            .HasForeignKey(vf => vf.StoredFileHash);

        // Configure the many-to-many relationship between StoredFile and ClassificationTag
        // EF Core will automatically create a join table called "ClassificationTagStoredFile"
        modelBuilder.Entity<StoredFile>()
            .HasMany(sf => sf.ClassificationTags)
            .WithMany(); // If ClassificationTag does not need a navigation property back to StoredFile.
                         // If it does, you would add `WithMany(ct => ct.StoredFiles)`
    }
}
