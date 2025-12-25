using IIM.Shared.Models;
using IIM.Shared.Models.Core;
using Microsoft.EntityFrameworkCore;

namespace IIM.Infrastructure.Data;

public class GovernanceDbContext : DbContext
{
    public DbSet<ClassificationTag> ClassificationTags { get; set; }
    public DbSet<StorageTier> StorageTiers { get; set; }
    public DbSet<AccessRole> AccessRoles { get; set; }
    public DbSet<AccessControlRule> AccessControlRules { get; set; }

    public GovernanceDbContext(DbContextOptions<GovernanceDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Configure relationships and constraints here if needed
        modelBuilder.Entity<AccessControlRule>()
            .HasOne(r => r.AccessRole)
            .WithMany()
            .HasForeignKey(r => r.AccessRoleId);

        modelBuilder.Entity<AccessControlRule>()
            .HasOne(r => r.ClassificationTag)
            .WithMany()
            .HasForeignKey(r => r.ClassificationTagId);

		modelBuilder.Entity<StoredFile>()
	.HasMany(f => f.ClassificationTags)
	.WithMany(t => t.StoredFiles)
	.UsingEntity(j => j.ToTable("StoredFileClassificationTags"));

	}
}
