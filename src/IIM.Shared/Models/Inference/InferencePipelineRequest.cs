using System.Collections.Generic;
using IIM.Shared.DTOs;

namespace IIM.Shared.Models
{
    /// <summary>
    /// Internal request for inference pipeline
    /// </summary>
    public class InferencePipelineRequest
    {
        public string? ModelId { get; set; }
        public ModelCapability? RequiredCapability { get; set; }
        public object Input { get; set; } = new { };
        public Dictionary<string, object>? Parameters { get; set; }
        public HashSet<string>? Tags { get; set; }
        public int Priority { get; set; } = 1;
        public bool Stream { get; set; } = false;

        /// <summary>
        /// Create request for specific model
        /// </summary>
        public static InferencePipelineRequest ForModel(string modelId, object input)
        {
            return new InferencePipelineRequest
            {
                ModelId = modelId,
                Input = input
            };
        }

        /// <summary>
        /// Create request by capability
        /// </summary>
        public static InferencePipelineRequest ForCapability(ModelCapability capability, object input)
        {
            return new InferencePipelineRequest
            {
                RequiredCapability = capability,
                Input = input
            };
        }
    }
}