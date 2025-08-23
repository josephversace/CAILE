using IIM.Core.Mediator;
using IIM.Shared.DTOs;
using System.ComponentModel.DataAnnotations;

namespace IIM.Application.Commands.Investigation
{
    /// <summary>
    /// Command to enrich a response with display metadata and visualization recommendations.
    /// </summary>
    public class EnrichResponseCommand : IRequest<InvestigationResponse>
    {
        /// <summary>
        /// Gets the response to enrich.
        /// </summary>
        [Required]
        public InvestigationResponse Response { get; }

        /// <summary>
        /// Gets optional message for additional context.
        /// </summary>
        public InvestigationMessage? Message { get; }

        /// <summary>
        /// Gets whether to generate visualization recommendations.
        /// </summary>
        public bool GenerateVisualizations { get; }

        /// <summary>
        /// Initializes a new instance of the EnrichResponseCommand.
        /// </summary>
        /// <param name="response">Response to enrich</param>
        /// <param name="message">Optional message context</param>
        /// <param name="generateVisualizations">Whether to generate visualizations</param>
        public EnrichResponseCommand(
            InvestigationResponse response,
            InvestigationMessage? message = null,
            bool generateVisualizations = true)
        {
            Response = response ?? throw new ArgumentNullException(nameof(response));
            Message = message;
            GenerateVisualizations = generateVisualizations;
        }
    }
}