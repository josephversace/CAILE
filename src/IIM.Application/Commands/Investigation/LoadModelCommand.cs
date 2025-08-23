using IIM.Core.Mediator;
using IIM.Shared.Enums;
using IIM.Shared.DTOs;

namespace IIM.Application.Commands.Models
{
    /// <summary>
    /// Command to load an AI model into memory.
    /// NOTE: This should already exist based on the API usage.
    /// </summary>
    public class LoadModelCommand : IRequest<ModelHandle>
    {
        public string ModelId { get; set; } = string.Empty;
        public string? ModelPath { get; set; }
        public ModelType ModelType { get; set; }
        public string? ModelSize { get; set; }
        public string? Quantization { get; set; }
        public int? ContextLength { get; set; }
        public int? DeviceId { get; set; }
        public ModelPriority? Priority { get; set; }
        public Dictionary<string, object>? Parameters { get; set; }
        public bool PreloadToGpu { get; set; } = true;
        public long? MaxMemory { get; set; }
    }

    /// <summary>
    /// Command to unload a model from memory.
    /// NOTE: This already exists based on your UnloadModelCommandHandler.
    /// </summary>
    public class UnloadModelCommand : IRequest<bool>
    {
        public string ModelId { get; set; } = string.Empty;
        public bool Force { get; set; } = false;
    }

    /// <summary>
    /// Notification when model is unloaded.
    /// NOTE: This already exists in the UnloadModelCommandHandler.
    /// </summary>
    public class ModelUnloadedNotification : INotification
    {
        public string ModelId { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
    }
}