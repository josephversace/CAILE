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
		public DbSet<ModelParameterSet> ModelParameterSets { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			//
			// 🔧 JSON Value Converter for Dictionary<string, object>
			//
			var dictionaryConverter = new ValueConverter<Dictionary<string, object>, string>(
				v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
				v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions)null) ?? new()
			);

			//
			// 📌 ModelConfiguration.Properties
			//
			modelBuilder.Entity<ModelConfiguration>()
				.Property(e => e.Properties)
				.HasConversion(dictionaryConverter)
				.HasColumnType("nvarchar(max)");

			//
			// 📌 ModelConfiguration.Parameters
			//
			modelBuilder.Entity<ModelConfiguration>()
				.Property(e => e.Parameters)
				.HasConversion(dictionaryConverter)
				.HasColumnType("nvarchar(max)");

			//
			// 📌 ModelParameterSet.Parameters
			//
			modelBuilder.Entity<ModelParameterSet>()
				.Property(e => e.Parameters)
				.HasConversion(dictionaryConverter)
				.HasColumnType("nvarchar(max)");

			//
			// 🚫 EXCLUDE: ModelCapabilities should NOT be treated as an EF entity
			//
			modelBuilder.Ignore<ModelCapabilities>();
		}
	}
}
