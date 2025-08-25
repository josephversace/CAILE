using IIM.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace IIM.Infrastructure.Data
{

    public class ModelDbContext : DbContext
    {
        public ModelDbContext(DbContextOptions<ModelDbContext> options) : base(options) { }

        public DbSet<ModelConfiguration> ModelConfigurations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Value converter for Dictionary<string, object>
            var dictionaryConverter = new ValueConverter<Dictionary<string, object>, string>(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions)null) ?? new()
            );

            modelBuilder.Entity<ModelConfiguration>()
                .Property(e => e.Properties)
                .HasConversion(dictionaryConverter);

            modelBuilder.Entity<ModelConfiguration>()
                .Property(e => e.Parameters)
                .HasConversion(dictionaryConverter);

            // Add if you want to specify column types (optional, but helpful for migrations)
            modelBuilder.Entity<ModelConfiguration>()
                .Property(e => e.Properties)
                .HasColumnType("nvarchar(max)");

            modelBuilder.Entity<ModelConfiguration>()
                .Property(e => e.Parameters)
                .HasColumnType("nvarchar(max)");
        }
    }
}

