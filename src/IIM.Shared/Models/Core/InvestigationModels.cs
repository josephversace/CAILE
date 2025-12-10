using System;
using System.Collections.Generic;

namespace IIM.Shared.Models
{
    /// <summary>
    /// Represents a single chat-based investigation session within a workspace.
    /// </summary>
    public class InvestigationSession
    {
        public Guid Id { get; set; }
        public Guid WorkspaceId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;
        public List<Message> Messages { get; set; } = new();
    }

	/// <summary>
	/// Represents a single message within an investigation session.
	/// </summary>
	public record Artifact(string Type, string Title, string Time, string Summary, string CssType = null)
	{
		public string TypeCss => CssType ?? Type.ToLower();
	}

	public record MemoryItem(string Icon, string Label, string Meta);
	public record AgentItem(string Name, string Note, string Status);

	public class Message
	{
		public string Id { get; set; } = Guid.NewGuid().ToString();
		public string Role { get; set; } = "user";
		public string Text { get; set; } = "";
		public string Author { get; set; } = "";
		public string? Tag { get; set; }
		public bool IsPinned { get; set; }
		public DateTime Timestamp { get; set; } = DateTime.Now;

		// Mode indicator
		public bool IsReasoning { get; set; }

		public string HiddenReasoning { get; set; }


		// Attachments
		public List<MessageAttachment>? Attachments { get; set; }

		public Message() { }

		public Message(string role, string text, string author)
		{
			Role = role;
			Text = text;
			Author = author;
		}

		public Message(string role, string text, string author, string? tag = null, bool isPinned = false)
		{
			Role = role;
			Text = text;
			Author = author;
			Tag = tag;
			IsPinned = isPinned;
		}


	}


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
