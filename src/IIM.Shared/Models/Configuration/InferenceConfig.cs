namespace IIM.Shared.Models;

public class InferenceConfig
{
	public int MaxConcurrentInferences { get; set; }
	public int DefaultTimeoutSeconds { get; set; }
	public string ModelCachePath { get; set; } = "";
	public bool EnableGpuAcceleration { get; set; }
	public string DefaultProvider { get; set; } = "CPU";
}
