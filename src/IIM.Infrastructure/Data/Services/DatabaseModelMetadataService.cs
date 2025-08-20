
using IIM.Infrastructure.Data.Entities;
using IIM.Shared.Enums;
using IIM.Shared.Models; // Model from Shared
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Infrastructure.Data.Services
{
    /// <summary>
    /// Database-backed implementation of model metadata service using SQLite
    /// </summary>
    public class DatabaseModelMetadataService : IModelMetadataService
    {
        private readonly ILogger<DatabaseModelMetadataService> _logger;
        private readonly IIMDbContext _context;

        public DatabaseModelMetadataService(
            ILogger<DatabaseModelMetadataService> logger,
            IIMDbContext context)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Shared.Models.ModelMetadata> GetMetadataAsync(string modelId, CancellationToken ct = default)
        {
            var entity = await _context.ModelMetadata
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ModelId == modelId && m.IsEnabled, ct);

            if (entity != null)
            {
                return MapToModelMetadata(entity);
            }

            _logger.LogWarning("Metadata not found for model {ModelId}, returning defaults", modelId);
            return CreateDefaultMetadata(modelId);
        }

        public async Task RegisterMetadataAsync(Shared.Models.ModelMetadata metadata, CancellationToken ct = default)
        {
            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            var entity = await _context.ModelMetadata
                .FirstOrDefaultAsync(m => m.ModelId == metadata.ModelId, ct);

            if (entity == null)
            {
                entity = new ModelMetadataEntity
                {
                    ModelId = metadata.ModelId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.ModelMetadata.Add(entity);
                _logger.LogInformation("Registering new model metadata for {ModelId}", metadata.ModelId);
            }
            else
            {
                entity.UpdatedAt = DateTime.UtcNow;
                _logger.LogInformation("Updating model metadata for {ModelId}", metadata.ModelId);
            }

            // Update properties
            entity.ModelPath = metadata.ModelPath;
            entity.Type = metadata.Type;
            entity.RequiresGpu = metadata.RequiresGpu;
            entity.SupportsBatching = metadata.SupportsBatching;
            entity.MaxBatchSize = metadata.MaxBatchSize;
            entity.EstimatedMemoryMb = metadata.EstimatedMemoryMb;
            entity.DefaultPriority = metadata.DefaultPriority;
            entity.Provider = metadata.Provider;
            entity.Properties = metadata.Properties;

            await _context.SaveChangesAsync(ct);
        }

        public async Task<List<Shared.Models.ModelMetadata>> GetAllMetadataAsync(CancellationToken ct = default)
        {
            var entities = await _context.ModelMetadata
                .AsNoTracking()
                .Where(m => m.IsEnabled)
                .OrderBy(m => m.ModelId)
                .ToListAsync(ct);

            return entities.Select(MapToModelMetadata).ToList();
        }

        public async Task LoadFromConfigurationAsync(CancellationToken ct = default)
        {
            await _context.Database.EnsureCreatedAsync(ct);
            var count = await _context.ModelMetadata.CountAsync(ct);
            _logger.LogInformation("Database contains metadata for {Count} models", count);
        }

        private Shared.Models.ModelMetadata MapToModelMetadata(ModelMetadataEntity entity)
        {
            return new Shared.Models.ModelMetadata
            {
                ModelId = entity.ModelId,
                ModelPath = entity.ModelPath ?? string.Empty,
                Type = entity.Type,
                RequiresGpu = entity.RequiresGpu,
                SupportsBatching = entity.SupportsBatching,
                MaxBatchSize = entity.MaxBatchSize,
                EstimatedMemoryMb = entity.EstimatedMemoryMb,
                DefaultPriority = entity.DefaultPriority,
                Provider = entity.Provider,
                Properties = entity.Properties
            };
        }

        private Shared.Models.ModelMetadata CreateDefaultMetadata(string modelId)
        {
            return new Shared.Models.ModelMetadata
            {
                ModelId = modelId,
                Type = InferModelType(modelId),
                RequiresGpu = true,
                SupportsBatching = false,
                MaxBatchSize = 1,
                EstimatedMemoryMb = 1000,
                DefaultPriority = 1,
                Provider = "cpu"
            };
        }

        private ModelType InferModelType(string modelId)
        {
            var lower = modelId.ToLowerInvariant();

            if (lower.Contains("whisper"))
                return ModelType.Whisper;

            if (lower.Contains("embedding") || lower.Contains("bert") || lower.Contains("minilm"))
                return ModelType.Embedding;

            // Note: ModelType.Vision might not exist in your enums
            // Use LLM as fallback for vision models for now
            if (lower.Contains("vision") || lower.Contains("clip"))
                return ModelType.LLM; // TODO: Add Vision to ModelType enum if needed

            return ModelType.LLM;
        }
    }
}
