using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace IIM.Shared.Models
{
	public class AGUIRequest
	{
		[JsonPropertyName("threadId")] public string ThreadId { get; set; } = "";
		[JsonPropertyName("runId")] public string RunId { get; set; } = "";
		[JsonPropertyName("messages")] public List<AGUIMessage> Messages { get; set; } = new();
		[JsonPropertyName("context")] public List<object> Context { get; set; } = new();

		// Already retrieved - don't send again
		[JsonPropertyName("retrievedChunks")] public List<string>? RetrievedChunks { get; set; }
		[JsonPropertyName("retrievedEntities")] public List<string>? RetrievedEntities { get; set; }
		[JsonPropertyName("retrievedRelationships")] public List<string>? RetrievedRelationships { get; set; }
	}

	public class AGUIMessage
	{
		[JsonPropertyName("id")] public string Id { get; set; } = "";
		[JsonPropertyName("role")] public string Role { get; set; } = "";
		[JsonPropertyName("content")] public string Content { get; set; } = "";
		[JsonPropertyName("name")] public string? Name { get; set; }
	}
}
