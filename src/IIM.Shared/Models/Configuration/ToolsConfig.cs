namespace IIM.Shared.Models.Configuration;

public sealed class ToolsConfig
{
	public ExifToolConfig ExifTool { get; set; } = new();

	// Future-ready
	public FfmpegConfig? Ffmpeg { get; set; }
	public bool ValidateOnStartup { get; set; } = true;
}

public sealed class ExifToolConfig
{
	/// <summary>
	/// Absolute path to the ExifTool executable.
	/// Installer-owned, not user-editable.
	/// </summary>
	public string Path { get; set; }

	/// <summary>
	/// Version string captured at install time (e.g. "13.45").
	/// </summary>
	public string Version { get; set; }

	/// <summary>
	/// If true, ingestion will fail if ExifTool is unavailable.
	/// </summary>
	public bool Required { get; set; } = true;

	/// <summary>
	/// Default execution profile (Fast, Full, Forensic, etc.)
	/// </summary>
	public string DefaultProfile { get; set; } = "Fast";
}

public sealed class FfmpegConfig
{
	public string Path { get; set; }
	public string Version { get; set; }
	public bool Required { get; set; } = false;
}