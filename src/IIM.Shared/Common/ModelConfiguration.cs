using IIM.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Shared.Models
{
    public class ModelConfiguration
    {
        public string ModelId { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public ModelType Type { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
        public ModelStatus Status { get; set; }
        public long MemoryUsage { get; set; }
        public string? LoadedPath { get; set; }
        public DateTimeOffset? LoadedAt { get; set; }
        public ModelCapabilities Capabilities { get; set; } = new();
        public string Name { get; set; } = string.Empty;
    }

    public class ModelCapabilities
    {
        public int MaxContextLength { get; set; }
        public List<string> SupportedLanguages { get; set; } = new();
        public List<string> SpecialFeatures { get; set; } = new();
        public bool SupportsStreaming { get; set; }
        public bool SupportsFineTuning { get; set; }
        public bool SupportsMultiModal { get; set; }
        public Dictionary<string, object> CustomCapabilities { get; set; } = new();
    }

}
