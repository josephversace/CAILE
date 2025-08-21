// File: SemanticKernelOrchestrator.ModelRouting.cs
using IIM.Shared.Enums;
using IIM.Shared.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Core.AI
{
    public partial class SemanticKernelOrchestrator
    {
  
   

        /// <summary>
        /// Filters models by specified constraints.
        /// </summary>
        /// <param name="models">List of model configurations.</param>
        /// <param name="constraints">Constraints object.</param>
        /// <returns>Filtered model list.</returns>
        private List<ModelConfiguration> FilterByConstraints(
               List<ModelConfiguration> models,
               ModelConstraints constraints)
        {
            var filtered = models.AsEnumerable();

            if (constraints.MaxMemoryBytes.HasValue)
            {
                filtered = filtered.Where(m => m.MemoryUsage <= constraints.MaxMemoryBytes.Value);
            }

            if (constraints.PreferLocal)
            {
                filtered = filtered.Where(m =>
                    m.Provider?.ToLowerInvariant() != "openai" &&
                    m.Provider?.ToLowerInvariant() != "azure");
            }

            return filtered.ToList();
        }
        /// <summary>
        /// Selects the best model for a specific task.
        /// </summary>
        /// <param name="task">Task string.</param>
        /// <param name="candidates">List of candidate models.</param>
        /// <returns>Selected model configuration.</returns>
        private ModelConfiguration? SelectBestModelForTask(string task, List<ModelConfiguration> candidates)
        {
            var taskLower = task.ToLowerInvariant();

            // Match task to model type
            ModelType preferredType = taskLower switch
            {
                var t when t.Contains("transcribe") || t.Contains("audio") => ModelType.Whisper,
                var t when t.Contains("image") || t.Contains("vision") => ModelType.CLIP,
                var t when t.Contains("embed") || t.Contains("similarity") => ModelType.Embedding,
                var t when t.Contains("ocr") || t.Contains("text extract") => ModelType.OCR,
                _ => ModelType.LLM
            };

            // Find best match
            return candidates.FirstOrDefault(m => m.Type == preferredType) ??
                   candidates.FirstOrDefault();
        }

        /// <summary>
        /// Determines if a model is compatible with the specified task.
        /// </summary>
        /// <param name="task">Task string.</param>
        /// <param name="model">Model configuration.</param>
        /// <returns>True if compatible, otherwise false.</returns>
        private bool IsCompatibleWithTask(string task, ModelConfiguration model)
        {
            var taskLower = task.ToLowerInvariant();

            return (model.Type, taskLower) switch
            {
                (ModelType.Whisper, var t) when t.Contains("audio") || t.Contains("transcribe") => true,
                (ModelType.CLIP, var t) when t.Contains("image") || t.Contains("vision") => true,
                (ModelType.Embedding, var t) when t.Contains("embed") || t.Contains("similar") => true,
                (ModelType.LLM, _) => true, // LLMs are generally compatible with most tasks
                _ => false
            };
        }

        /// <summary>
        /// Calculates confidence score for model-task compatibility.
        /// </summary>
        /// <param name="task">Task string.</param>
        /// <param name="model">Model configuration.</param>
        /// <returns>Confidence score (0-1).</returns>
        private float CalculateModelConfidence(string task, ModelConfiguration model)
        {
            if (IsCompatibleWithTask(task, model))
            {
                return model.Status == ModelStatus.Loaded ? 0.95f : 0.75f;
            }

            return 0.3f;
        }

        /// <summary>
        /// Generates recommended model parameters for a task.
        /// </summary>
        /// <param name="task">Task string.</param>
        /// <param name="model">Model configuration.</param>
        /// <returns>Dictionary of parameters.</returns>
        private Dictionary<string, object> GenerateModelParameters(string task, ModelConfiguration model)
        {
            var parameters = new Dictionary<string, object>();

            switch (model.Type)
            {
                case ModelType.LLM:
                    parameters["temperature"] = 0.7;
                    parameters["max_tokens"] = 2048;
                    parameters["top_p"] = 0.9;
                    break;

                case ModelType.Whisper:
                    parameters["language"] = "en";
                    parameters["task"] = "transcribe";
                    break;

                case ModelType.Embedding:
                    parameters["normalize"] = true;
                    break;
            }

            return parameters;
        }
    }
}
