using System;
using Microsoft.EntityFrameworkCore;
using IIM.Infrastructure.Data.Entities;
using IIM.Shared.Enums;

namespace IIM.Infrastructure.Data
{
    /// <summary>
    /// Lightweight SQLite database context for IIM configuration and metadata
    /// </summary>
    public class IIMDbContext : DbContext
    {
        public IIMDbContext(DbContextOptions<IIMDbContext> options) : base(options)
        {
        }

        // Configuration entities
        public DbSet<ModelMetadataEntity> ModelMetadata { get; set; }
        public DbSet<AuditLogEntity> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure ModelMetadata
            modelBuilder.Entity<ModelMetadataEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.ModelId).IsUnique();
                entity.Property(e => e.ModelId).IsRequired().HasMaxLength(200);
                entity.Property(e => e.ModelPath).HasMaxLength(500);
                entity.Property(e => e.Provider).HasMaxLength(50);
                entity.Property(e => e.PropertiesJson);
            });

            // Seed default model metadata
            modelBuilder.Entity<ModelMetadataEntity>().HasData(
                new ModelMetadataEntity
                {
                    Id = 1,
                    ModelId = "llama3.1:70b",
                    Type = ModelType.LLM,
                    RequiresGpu = true,
                    SupportsBatching = false,
                    MaxBatchSize = 1,
                    EstimatedMemoryMb = 40000,
                    DefaultPriority = 1,
                    Provider = "ollama",
                    CreatedAt = DateTime.UtcNow,
                    PropertiesJson = "{}"
                },
                new ModelMetadataEntity
                {
                    Id = 2,
                    ModelId = "phi-3-mini",
                    Type = ModelType.LLM,
                    RequiresGpu = true,
                    SupportsBatching = false,
                    MaxBatchSize = 1,
                    EstimatedMemoryMb = 4000,
                    DefaultPriority = 1,
                    Provider = "directml",
                    ModelPath = "Models/phi-3-mini.onnx",
                    CreatedAt = DateTime.UtcNow,
                    PropertiesJson = "{}"
                },
                new ModelMetadataEntity
                {
                    Id = 3,
                    ModelId = "whisper-base",
                    Type = ModelType.Whisper,
                    RequiresGpu = true,
                    SupportsBatching = false,
                    MaxBatchSize = 1,
                    EstimatedMemoryMb = 150,
                    DefaultPriority = 2,
                    Provider = "directml",
                    ModelPath = "Models/whisper-base.onnx",
                    CreatedAt = DateTime.UtcNow,
                    PropertiesJson = "{}"
                },
                new ModelMetadataEntity
                {
                    Id = 4,
                    ModelId = "all-MiniLM-L6-v2",
                    Type = ModelType.Embedding,
                    RequiresGpu = false,
                    SupportsBatching = true,
                    MaxBatchSize = 32,
                    EstimatedMemoryMb = 100,
                    DefaultPriority = 1,
                    Provider = "cpu",
                    ModelPath = "Models/all-MiniLM-L6-v2.onnx",
                    CreatedAt = DateTime.UtcNow,
                    PropertiesJson = "{}"
                }
            );

            // Configure AuditLogs
            modelBuilder.Entity<AuditLogEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => e.EventType);
                entity.Property(e => e.EventType).IsRequired().HasMaxLength(100);
                entity.Property(e => e.UserId).HasMaxLength(100);
                // Remove .HasColumnName() - not needed for SQLite
                entity.Property(e => e.DetailsJson);
            });

        }
    }
}