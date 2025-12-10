namespace IIM.Shared.Models;

public class DataRouterConfig
{
	public bool EnableDeduplication { get; set; }
	public bool EnableQuarantine { get; set; }
	public int DefaultQuarantineDays { get; set; }
	public string[] HashAlgorithms { get; set; } = new string[0];
	public PerceptualHashingConfig PerceptualHashing { get; set; } = new();
}

public class PerceptualHashingConfig
{
	public bool Enabled { get; set; }
	public string Algorithm { get; set; } = "";
	public double SimilarityThreshold { get; set; }
}
