 using IIM.Core.Inference;
    using IIM.Core.Models;
    using Microsoft.Extensions.Logging;

namespace IIM.Core.Services
{
   

    public interface IInferenceService
    {
        Task<string> InferAsync(string prompt, CancellationToken cancellationToken = default);
        Task<T> GenerateAsync<T>(string modelId, object input, CancellationToken cancellationToken = default);
    }

    public class InferenceService : IInferenceService
    {
        private readonly IInferencePipeline _pipeline;
        private readonly ILogger<InferenceService> _logger;

        public InferenceService(IInferencePipeline pipeline, ILogger<InferenceService> logger)
        {
            _pipeline = pipeline;
            _logger = logger;
        }

        public async Task<string> InferAsync(string prompt, CancellationToken cancellationToken = default)
        {
            var request = new InferencePipelineRequest
            {
                ModelId = "default",
                Input = prompt
            };

            return await _pipeline.ExecuteAsync<string>(request, cancellationToken);
        }

        public async Task<T> GenerateAsync<T>(string modelId, object input, CancellationToken cancellationToken = default)
        {
            var request = new InferencePipelineRequest
            {
                ModelId = modelId,
                Input = input
            };

            return await _pipeline.ExecuteAsync<T>(request, cancellationToken);
        }
    }
}