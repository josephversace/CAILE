
namespace IIM.Core.Models;

public class IIMException : Exception
{
    public string ErrorCode { get; set; }
    public Dictionary<string, object>? Context { get; set; }

    public IIMException(string message, string errorCode = "IIM_ERROR") : base(message)
    {
        ErrorCode = errorCode;
    }

    public IIMException(string message, Exception innerException, string errorCode = "IIM_ERROR")
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}

public class EvidenceNotFoundException : IIMException
{
    public EvidenceNotFoundException(string evidenceId)
        : base($"Evidence with ID {evidenceId} not found", "EVIDENCE_NOT_FOUND")
    {
        Context = new Dictionary<string, object> { ["evidenceId"] = evidenceId };
    }
}

public class IntegrityException : IIMException
{
    public IntegrityException(string message)
        : base(message, "INTEGRITY_VIOLATION")
    {
    }
}

public class ModelNotLoadedException : IIMException
{
    public ModelNotLoadedException(string modelId)
        : base($"Model {modelId} is not loaded", "MODEL_NOT_LOADED")
    {
        Context = new Dictionary<string, object> { ["modelId"] = modelId };
    }
}

public class InsufficientMemoryException : IIMException
{
    public InsufficientMemoryException(long required, long available)
        : base($"Insufficient memory: {required} bytes required, {available} bytes available", "INSUFFICIENT_MEMORY")
    {
        Context = new Dictionary<string, object>
        {
            ["required"] = required,
            ["available"] = available
        };
    }
}

public class ToolExecutionException : IIMException
{
    public ToolExecutionException(string message, Exception? innerException = null)
        : base(message, innerException!, "TOOL_EXECUTION_FAILED")
    {
    }
}