using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Application.AI.DataEnrichment.Models
{
    public class AnalysisRequest
    {
        public string RequestId { get; set; } = Guid.NewGuid().ToString();
        public string FileName { get; set; } = string.Empty;
        public Stream Content { get; set; } = Stream.Null;
        public string MimeType { get; set; } = string.Empty;
        public AnalysisOptions Options { get; set; } = new();
        public Guid? WorkspaceId { get; set; }
        public string? UserId { get; set; }
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object>? Metadata { get; set; }
    }

    public class AnalysisOptions
    {
        public bool ExtractText { get; set; } = true;
        public bool ExtractMetadata { get; set; } = true;
        public bool GenerateEmbeddings { get; set; } = true;
        public bool ClassifyContent { get; set; } = true;
        public bool AssessSensitivity { get; set; } = true;
        public bool ExtractEntities { get; set; } = true;
        public int MaxTextLength { get; set; } = 10000;
        public float ConfidenceThreshold { get; set; } = 0.7f;
        public Dictionary<string, object>? CustomOptions { get; set; }
    }
}
