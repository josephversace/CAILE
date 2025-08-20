using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using IIM.Shared.Enums;

namespace IIM.Infrastructure.Data.Entities
{
    /// <summary>
    /// Database entity for model metadata
    /// </summary>
    public class ModelMetadataEntity
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string ModelId { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? ModelPath { get; set; }

        public ModelType Type { get; set; }

        public bool RequiresGpu { get; set; }

        public bool SupportsBatching { get; set; }

        public int MaxBatchSize { get; set; } = 1;

        public long EstimatedMemoryMb { get; set; }

        public int DefaultPriority { get; set; } = 1;

        [MaxLength(50)]
        public string Provider { get; set; } = "cpu";

        public bool IsEnabled { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Store additional properties as JSON
        public string PropertiesJson { get; set; } = "{}";

        // Helper property to work with properties
        public Dictionary<string, object> Properties
        {
            get => string.IsNullOrEmpty(PropertiesJson)
                ? new Dictionary<string, object>()
                : JsonSerializer.Deserialize<Dictionary<string, object>>(PropertiesJson) ?? new();
            set => PropertiesJson = JsonSerializer.Serialize(value);
        }
    }
}
