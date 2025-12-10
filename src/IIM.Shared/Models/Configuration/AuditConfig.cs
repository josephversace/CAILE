namespace IIM.Shared.Models;

public class AuditConfig
{
	public string LogPath { get; set; } = "";
	public bool EnableDetailedLogging { get; set; }
	public int RetentionDays { get; set; }
	public string LogLevel { get; set; } = "Information";
}
