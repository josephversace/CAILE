namespace IIM.Shared.Models;

public class LoggingConfig
{
	public LoggingLevels LogLevel { get; set; } = new();
}

public class LoggingLevels
{
	public string Default { get; set; } = "Information";
	public string Microsoft { get; set; } = "Warning";
	public string MicrosoftAspNetCore { get; set; } = "Warning";
	public string IIM { get; set; } = "Debug";
}
