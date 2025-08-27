using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Shared.Models
{
    public class RagResponse
    {
        // Existing core properties
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Query { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public double Confidence { get; set; }

        // New optional properties
        public List<RetrievedChunk>? RetrievedChunks { get; set; }  // Retrieved document chunks
        public List<object>? Chunks { get; set; }  // Generic chunks for compatibility
        public List<Source>? Sources { get; set; }  // Source documents
        public Dictionary<string, double>? SourceScores { get; set; }  // Relevance scores
        public TimeSpan? RetrievalTime { get; set; }  // Time to retrieve
        public TimeSpan? GenerationTime { get; set; }  // Time to generate answer
        public int TokensUsed { get; set; }
        public TimeSpan ProcessingTime { get; set; }
    }

    public class RetrievedChunk
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Source { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public int PageNumber { get; set; }
        public double Relevance { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }

    public class Source
    {
        public string Document { get; set; }
        public int Page { get; set; }
        public float Relevance { get; set; }
    }

    #region Images


    public class ImageSearchResults
    {
        public List<ImageMatch> Matches { get; set; } = new();
        public TimeSpan QueryProcessingTime { get; set; }
        public int TotalImagesSearched { get; set; }
    }


    public class ImageMatch
    {
        public string ImagePath { get; set; } = string.Empty;
        public float Score { get; set; }
     
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    #endregion

}
