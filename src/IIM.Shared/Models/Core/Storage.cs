using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Shared.Models
{
    /// <summary>
    /// Enum for model source locations
    /// </summary>
    public enum ModelSource
    {
        Auto,       // Search all locations
        System,     // System-provided models
        User,       // User-imported models
        Cache,      // Downloaded from hub
        FineTuned   // Fine-tuned models
    }

    /// <summary>
    /// Enum for template types
    /// </summary>
    public enum TemplateType
    {
        Model,          // Model configuration templates
        Workflow,       // Investigation workflow templates
        Report,         // Report export templates
        Prompt,         // Prompt templates
        Tool,           // Tool configuration templates
        Case,           // Case starter templates
        Visualization,  // Visualization templates
        Query          // Query templates
    }

    /// <summary>
    /// Storage statistics
    /// </summary>
    public class StorageStatistics
    {
        public long TotalSize { get; set; }
        public long ModelsSize { get; set; }
        public long WorkspacesSize { get; set; }
        public long FilesSize { get; set; }
        public long VectorStoreSize { get; set; }
        public int TemplatesCount { get; set; }
        public int ModelsCount { get; set; }
        public int CasesCount { get; set; }

        /// <summary>
        /// Gets the total size in GB
        /// </summary>
        public double TotalSizeGB => TotalSize / (1024.0 * 1024.0 * 1024.0);

        /// <summary>
        /// Gets the models size in GB
        /// </summary>
        public double ModelsSizeGB => ModelsSize / (1024.0 * 1024.0 * 1024.0);
    }

    /// <summary>
    /// Validation result for storage configuration
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }

}
