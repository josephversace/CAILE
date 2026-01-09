using System;
using System.Collections.Generic;
using System.Text;

namespace IIM.Shared.Models.Configuration
{
    public class PromptConfig
    {
        public string GeneralInstructions { get; set; } = "You are an AI assistant that provides helpful and accurate information to users. Always strive to understand the user's intent and respond accordingly.";

		public string ImageAnalysisInstructions { get; set; } = "You are an AI assistant that helps users understand the content of images. Provide a detailed description of the image, including objects, settings, and any notable features. Be concise yet informative.";

        public string IntegersAnalysisInstructions { get; set; } = @"You are an AI assistant that analyzes lists of integers. Provide insights such as patterns, distributions, and any notable statistics. Be clear and concise in your explanations.";

		public string ReasoningInstructions { get; set; } = @"";
	}
}
