using IIM.Core.Mediator;
using IIM.Shared.Enums;
using Mediator;
using System;
using System.Collections.Generic;
using System.Text;

namespace IIM.Application.Inference
{

    public class InferencePipelineException : Exception
    {
        public InferencePipelineException(string message) : base(message) { }
        public InferencePipelineException(string message, Exception innerException) : base(message, innerException) { }
    }


    public class InferenceQueuedNotification : INotification
    {
        public string RequestId { get; set; } = string.Empty;
        public string ModelId { get; set; } = string.Empty;
        public Priority Priority { get; set; }
        public int QueueDepth { get; set; }
    }


    public class InferenceStartedNotification : INotification
    {
        public string RequestId { get; set; } = string.Empty;
        public string ModelId { get; set; } = string.Empty;
        public long QueueTimeMs { get; set; }
    }


    public class InferenceCompletedNotification : INotification
    {
        public string RequestId { get; set; } = string.Empty;
        public string ModelId { get; set; } = string.Empty;
        public long QueueTimeMs { get; set; }
        public long InferenceTimeMs { get; set; }
        public int TokensGenerated { get; set; }
    }


    public class InferenceFailedNotification : INotification
    {
        public string RequestId { get; set; } = string.Empty;
        public string ModelId { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public string ErrorType { get; set; } = string.Empty;
    }

}
