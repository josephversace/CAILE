namespace IIM.Shared.Models;

public class HangfireConfig
{
	public int WorkerCount { get; set; }
	public string[] Queues { get; set; } = new string[0];
	public int RetryAttempts { get; set; }
	public string DashboardUrl { get; set; } = "/hangfire";
}
