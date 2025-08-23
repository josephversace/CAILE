using IIM.Core.Mediator;
using IIM.Core.Models;
using IIM.Shared.Models;
using System.ComponentModel.DataAnnotations;

namespace IIM.Application.Commands.Models
{
    /// <summary>
    /// Command to load an AI model into memory.
    /// Used by Program.cs model management endpoints.
    /// </summary>
    public class LoadModelCommand : IRequest<ModelHandle>
    {
        /// <summary>
        /// Gets or sets the model ID to load.
        /// </summary>
        [Required]
        public string ModelId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets optional model parameters.
        /// </summary>
        public Dictionary<string, object>? Parameters { get; set; }

        /// <summary>
        /// Gets or sets the user requesting the load.
        /// </summary>
        public string? UserId { get; set; }
    }
}
