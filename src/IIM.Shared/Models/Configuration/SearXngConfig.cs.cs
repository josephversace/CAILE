namespace IIM.Shared.Models.Configuration
{
	public sealed class SearXngConfig
	{
		public string BaseUrl { get; set; } = "http://localhost:8081";
		public int TimeoutSeconds { get; set; } = 30;

		public string[] DefaultEngines { get; set; } =
		{
			"google",
			"bing",
			"duckduckgo"
		};

		public int SafeSearch { get; set; } = 1;
		public string Language { get; set; } = "en";
	}
}
