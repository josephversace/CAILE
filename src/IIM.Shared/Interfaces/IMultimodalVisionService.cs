using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces
{
	public interface IMultimodalVisionService
	{
		/// <summary>
		/// Initializes the vision model asynchronously.
		/// Must be called once at app startup.
		/// </summary>
		Task InitializeAsync(CancellationToken ct = default);

		/// <summary>
		/// Analyzes an image with a text prompt.
		/// </summary>
		Task<string> AnalyzeImageAsync(string prompt, byte[] imageBytes, CancellationToken ct = default);

		/// <summary>
		/// Analyzes an image with a text prompt using streaming output.
		/// </summary>
		IAsyncEnumerable<string> AnalyzeImageStreamingAsync(string prompt, byte[] imageBytes, CancellationToken ct = default);

		/// <summary>
		/// Indicates whether the model has finished loading and is ready.
		/// </summary>
		bool IsReady { get; }
	}
}
