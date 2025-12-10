using Microsoft.ML.OnnxRuntime;

public interface IOnnxExecutionProvider
{
	/// <summary>
	/// Execution provider name for ONNX Runtime GenAI.
	/// Examples: "DmlExecutionProvider", "CUDAExecutionProvider".
	/// </summary>
	string GenAiName { get; }

	/// <summary>
	/// Human-readable name (optional).
	/// </summary>
	string Name { get; }

	/// <summary>
	/// Applies the execution provider to classic ONNX Runtime.
	/// </summary>
	SessionOptions Configure(SessionOptions options);
}
