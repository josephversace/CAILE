// File: MockTextGenerationService.cs
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.TextGeneration;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Core.AI
{
    /// <summary>
    /// Mock text generation service for testing without actual models.
    /// </summary>
    internal class MockTextGenerationService : ITextGenerationService
    {
        private readonly string _modelId;
        private readonly ILogger _logger;

        public IReadOnlyDictionary<string, object?> Attributes => new Dictionary<string, object?>();

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="modelId">Model identifier.</param>
        /// <param name="logger">Logger instance.</param>
        public MockTextGenerationService(string modelId, ILogger logger)
        {
            _modelId = modelId;
            _logger = logger;
        }

        /// <summary>
        /// Gets text contents in response to a prompt.
        /// </summary>
        /// <param name="prompt">Prompt string.</param>
        /// <param name="executionSettings">Prompt execution settings.</param>
        /// <param name="kernel">Optional kernel.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Read-only list of text content.</returns>
        public async Task<IReadOnlyList<TextContent>> GetTextContentsAsync(
          string prompt,
          PromptExecutionSettings? executionSettings = null,
          Kernel? kernel = null,
          CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Mock text generation for prompt: {Prompt}", prompt);
            await Task.Delay(100, cancellationToken);

            return new List<TextContent>
            {
                new TextContent($"Mock response to: {prompt}")
            };
        }

        /// <summary>
        /// Gets streaming text contents in response to a prompt.
        /// </summary>
        /// <param name="prompt">Prompt string.</param>
        /// <param name="executionSettings">Prompt execution settings.</param>
        /// <param name="kernel">Optional kernel.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>StreamingTextContent enumerator.</returns>

        public async IAsyncEnumerable<StreamingTextContent> GetStreamingTextContentsAsync(
            string prompt,
            PromptExecutionSettings? executionSettings = null,
            Kernel? kernel = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Mock streaming generation for prompt: {Prompt}", prompt);

            var response = $"Mock streaming response to: {prompt}";
            foreach (var word in response.Split(' '))
            {
                await Task.Delay(50, cancellationToken);
                yield return new StreamingTextContent(word + " ");
            }
        }
    }
}
