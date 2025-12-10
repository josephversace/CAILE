using IIM.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace IIM.Infrastructure.Data
{
	public class ConfigDbContext : DbContext
	{
		public ConfigDbContext(DbContextOptions<ConfigDbContext> options) : base(options) { }

		public DbSet<Setting> Settings { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<Setting>(entity =>
			{
				entity.ToTable("Settings");
				entity.HasKey(x => x.Id);

				entity.OwnsMany(x => x.Metadata, nav =>
				{
					nav.ToTable("SettingMetadata");
					nav.WithOwner().HasForeignKey("SettingId");

					nav.Property(m => m.Id).ValueGeneratedOnAdd();
					nav.HasKey(m => m.Id);

					nav.Property(m => m.Key).HasMaxLength(200);
					nav.Property(m => m.Value).HasMaxLength(2000);
				});
			});
		}
	}
}
