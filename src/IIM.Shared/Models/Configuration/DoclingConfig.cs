namespace IIM.Shared.Models;

public class DoclingConfig
{
	public string BaseUrl { get; set; } = "";
	public int TimeoutSeconds { get; set; }
	public int MaxFileSizeMb { get; set; }
}
