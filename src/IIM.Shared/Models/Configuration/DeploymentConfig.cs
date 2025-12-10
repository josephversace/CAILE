namespace IIM.Shared.Models;

public class DeploymentConfig
{
	public string Tier { get; set; } = "mini";
	public string Mode { get; set; } = "ServerNode";
	public bool CanChangeMode { get; set; }
	public bool RequireAuth { get; set; }
	public string ApiUrl { get; set; } = "";
	public string AdminEmail { get; set; } = "";
	public bool IsDevelopment { get; set; }
}
