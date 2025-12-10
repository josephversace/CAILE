using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IIM.Shared.Models;

/// <summary>
/// Full data separation config for routing, tiers, and classification.
/// This is the core of your "StorageConfig" setup screen.
/// </summary>

	public class StorageConfig
	{
		public List<SeparationLevel> Levels { get; set; } = new();
		public List<RoutingRule> RoutingRules { get; set; } = new();
		public Dictionary<string, string> ExtensionRules { get; set; } = new();

		public bool HasLevels => Levels.Count > 0;

	public void AddLevel(string name, string path, bool isDefault = false, int replication = 1)
	{
		Levels.Add(new SeparationLevel(name, path, isDefault, replication));
	}


}


/// <summary>
/// A classification boundary — physical separation.
/// </summary>

public class SeparationLevel
{
	public string Name { get; set; }
	public List<string> Paths { get; set; } = new();
	public int Replication { get; set; }
	public bool IsDefault { get; set; }

	public List<string> Tags { get; set; } = new();

	public bool Encrypted { get; set; } = false;



	[JsonIgnore] public string? NewTagInput { get; set; } // used only in UI




	public SeparationLevel() { }

	public SeparationLevel(string name, List<string> paths, bool isDefault = false, int replication = 1)
	{
		Name = name;
		Paths = paths;
		IsDefault = isDefault;
		Replication = replication;
		Encrypted = false;
		Tags = new();
	}

	public SeparationLevel(string name, string path, bool isDefault = false, int replication = 1)
	{
		Name = name;
		Paths.Add(path);
		IsDefault = isDefault;
		Replication = replication;
		Encrypted = false;
		Tags = new();
	}
}




/// <summary>
/// Rules that bypass general intake and route files directly.
/// </summary>
public enum RuleType { AI, Hash, Human }

public class RoutingRule
{
	public RuleType Type { get; set; }
	public string Source { get; set; } = "*";   // wildcard default
	public string Destination { get; set; } = "default";

	public RoutingRule() { } // required for model-binding

	// Normal explicit rule
	public RoutingRule(RuleType type, string source, string destination)
	{
		Type = type;
		Source = source;
		Destination = destination;
	}

	// 🔥 NEW — UI SAFE VERSION (Fixes your CS7036 instantly)
	public RoutingRule(RuleType type, string destination)
	{
		Type = type;
		Destination = destination;
		Source = "*"; // default when user doesn't define source
	}
}
