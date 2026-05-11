using System.Diagnostics;

namespace IIM.Api.Services
{

	public sealed class RunTelemetry
	{
		public Stopwatch Stopwatch { get; } = Stopwatch.StartNew();

		// Prompt / context
		public int PromptCharCount { get; set; }
		public int ContextTokenEstimate { get; set; }

		// Generation
		public int CompletionCharCount { get; private set; }
		public int CompletionTokenEstimate => CompletionCharCount / 4;

		public void AddCompletionText(string text)
		{
			if (!string.IsNullOrEmpty(text))
				CompletionCharCount += text.Length;
		}

		public double TokensPerSecond =>
			Stopwatch.Elapsed.TotalSeconds > 0
				? CompletionTokenEstimate / Stopwatch.Elapsed.TotalSeconds
				: 0;
	}

}
