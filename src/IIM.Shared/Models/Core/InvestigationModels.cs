using System;
using System.Collections.Generic;

namespace IIM.Shared.Models
{
   
	/// <summary>
	/// Represents a single message within an investigation session.
	/// </summary>
	public record Artifact(string Type, string Title, string Time, string Summary, string CssType = null)
	{
		public string TypeCss => CssType ?? Type.ToLower();
	}

	public record MemoryItem(string Icon, string Label, string Meta);
	public record AgentItem(string Name, string Note, string Status);

	

	public class MessageAttachment
	{
		public string Id { get; set; } = Guid.NewGuid().ToString();
		public string FileName { get; set; } = "";
		public string ContentType { get; set; } = "application/octet-stream";
		public long Size { get; set; }
		public byte[]? Data { get; set; }

		/// <summary>
		/// Base64-encoded data for serialization/transfer
		/// </summary>
		public string? Base64Data => Data != null ? Convert.ToBase64String(Data) : null;
	}

	/// <summary>
	/// Represents a citation or source for information provided by the AI.
	/// </summary>
	public class Citation
    {
        public Guid Id { get; set; }
        public string SourceName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public double Relevance { get; set; }
    }

    /// <summary>
    /// Represents a tool call made by the AI during a reasoning step.
    /// </summary>
 
}
