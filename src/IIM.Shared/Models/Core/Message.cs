using System;
using System.Collections.Generic;
using IIM.Shared.Models;

namespace IIM.Shared.Models;

public class Message
{
	public string Id { get; set; } = Guid.NewGuid().ToString();
	public string Role { get; set; }
	public string? Text { get; set; }
	public string Sender { get; set; }
	public DateTime Timestamp { get; set; } = DateTime.UtcNow;

	public List<ProcessedAttachment>? Attachments { get; set; }

	public bool IsReasoning { get; set; }
	public string? HiddenReasoning { get; set; }
	public string? Tag { get; set; }

	public bool IsPinned { get; set; }

	/// <summary>
	/// Context chips attached to this message.
	/// Resolved to hashes before RAG query.
	/// </summary>
	public List<ContextChip> Context { get; set; } = [];

	/// <summary>
	/// Resolved hashes at send time (for tagged/saved messages).
	/// </summary>
	public List<string>? ResolvedHashes { get; set; }

	public Message(string role, string? text, string sender)
	{
		Role = role;
		Text = text;
		Sender = sender;
	}
}
