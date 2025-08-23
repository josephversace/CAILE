// File: SemanticKernelOrchestrator.KernelManagement.cs
using IIM.Shared.Enums;
using IIM.Shared.DTOs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.TextGeneration;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Models;

namespace IIM.Core.AI
{
    public partial class SemanticKernelOrchestrator
    {





        /// <summary>
        /// Configures the kernel builder for a specific purpose.
        /// </summary>
        /// <param name="builder">Kernel builder.</param>
        /// <param name="purpose">Purpose string.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        private async Task ConfigureKernelForPurposeAsync(
               IKernelBuilder builder,
               string purpose,
               CancellationToken cancellationToken)
        {
            // Check if we have loaded models
            var loadedModels = await _modelOrchestrator.GetLoadedModelsAsync(cancellationToken);
            var llmModel = loadedModels.FirstOrDefault(m => m.Type == ModelType.LLM);

            if (llmModel != null)
            {
                // Configure with actual model
                _logger.LogInformation("Configuring kernel with loaded model: {ModelId}", llmModel.ModelId);

                // For Ollama models
                if (llmModel.Provider?.ToLowerInvariant() == "ollama")
                {
                    builder.AddOpenAIChatCompletion(
                        modelId: llmModel.ModelId,
                        endpoint: new Uri("http://localhost:11434"),
                        apiKey: "ollama");
                }
                // Add other providers as needed
            }
            else
            {
                _logger.LogWarning("No LLM model loaded, using mock service");
                // Use mock service for testing
                builder.Services.AddSingleton<ITextGenerationService>(
                    new MockTextGenerationService("mock-model", _logger));
            }

            // Add plugins based on purpose
            if (purpose.Contains("forensic", StringComparison.OrdinalIgnoreCase))
            {
                builder.Plugins.AddFromType<ForensicAnalysisPlugin>("ForensicAnalysis");
            }
        }

        /// <summary>
        /// Initializes built-in plugins.
        /// </summary>
        private void InitializeBuiltInPlugins()
        {
            _availablePlugins["ForensicAnalysis"] = new PluginInfo
            {
                Name = "ForensicAnalysis",
                Description = "Digital forensics and evidence analysis",
                Version = "1.0.0",
                IsLoaded = true,
                Functions = new List<string>
                {
                    "AnalyzeEvidence",
                    "ExtractMetadata",
                    "VerifyIntegrity",
                    "GenerateTimeline"
                }
            };
        }

        /// <summary>
        /// Gets the list of plugin functions for a kernel/plugin.
        /// </summary>
        /// <param name="kernel">The kernel instance.</param>
        /// <param name="pluginName">The plugin name.</param>
        /// <returns>List of function names.</returns>
        private List<string> GetPluginFunctions(Kernel kernel, string pluginName)
        {
            try
            {
                var plugin = kernel.Plugins.FirstOrDefault(p => p.Name.Equals(pluginName, StringComparison.OrdinalIgnoreCase));
                return plugin?.Select(f => f.Name).ToList() ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }
    }
}
