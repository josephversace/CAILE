namespace IIM.Shared.Models.Configuration
{
	public sealed class PlaywrightConfig
	{
		public string BaseUrl { get; set; } = "http://localhost:5003";
		public int TimeoutSeconds { get; set; } = 30;
		public int RenderTimeoutSeconds { get; set; } = 15;
		public int MaxConcurrentPages { get; set; } = 2;
		public bool Enabled { get; set; } = true;
	}
}
