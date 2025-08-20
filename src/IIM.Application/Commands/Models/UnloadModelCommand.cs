using IIM.Core.Mediator;

namespace IIM.Application.Commands.Models
{
    /// <summary>
    /// Command to unload a model from memory
    /// </summary>
    public class UnloadModelCommand : IRequest<bool>
    {
        /// <summary>
        /// ID of the model to unload
        /// </summary>
        public string ModelId { get; set; } = string.Empty;

        /// <summary>
        /// Force unload even if model is in use
        /// </summary>
        public bool Force { get; set; } = false;
    }
}