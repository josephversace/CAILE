using IIM.Shared.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace IIM.Infrastructure.Data
{
	public class AuditDbContext : DbContext
	{
		public AuditDbContext(DbContextOptions<AuditDbContext> options) : base(options) { }

		// Example audit entity:
		public DbSet<AuditEvent> AuditLogs { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<AuditEvent>(entity =>
			{
				entity.ToTable("AuditEvents");
				entity.HasKey(e => e.Id);

				entity.OwnsMany(e => e.AdditionalData, nav =>
				{
					nav.ToTable("AuditMetadata");
					nav.WithOwner().HasForeignKey("AuditEventId");

					nav.Property(m => m.Id).ValueGeneratedOnAdd();
					nav.HasKey(m => m.Id);

					nav.Property(m => m.Key).HasMaxLength(200);
					nav.Property(m => m.Value).HasMaxLength(2000);
				});
			});

	
		}
	}
}
