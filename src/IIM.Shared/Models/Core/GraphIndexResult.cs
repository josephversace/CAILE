using System.Collections.Generic;

namespace IIM.Shared.Models
{
	public class GraphRagIndexResult
	{
		public int ChunkCount { get; set; }
		public int EntityCount { get; set; }

		public List<TextUnitResult> TextUnits { get; set; } = new();
		public List<EntityResult> Entities { get; set; } = new();
		public List<RelationshipResult> Relationships { get; set; } = new();

		public string? GlobalSummary { get; set; }
		public Dictionary<string, object>? DebugInfo { get; set; }
	}

	public class TextUnitResult
	{
		public string Id { get; set; } = "";
		public string Text { get; set; } = "";
		public int Order { get; set; }
	}

	public class EntityResult
	{
		public string Name { get; set; } = "";
		public string Type { get; set; } = "";
		public double Score { get; set; }
	}

	public class RelationshipResult
	{
		public string Source { get; set; } = "";
		public string Target { get; set; } = "";
		public string Label { get; set; } = "";
		public double Weight { get; set; }
	}
}
