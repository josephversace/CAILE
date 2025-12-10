using Microsoft.ML.OnnxRuntime;

public class MetalProvider : IOnnxExecutionProvider
{
	// Used by ONNX Runtime GenAI (LLMs, multimodal models)
	public string GenAiName => "MetalExecutionProvider";

	// Human-readable label
	public string Name => "Metal";

	// Classic ORT does NOT support Metal in .NET.
	// Use CPU fallback.
	public SessionOptions Configure(SessionOptions options)
	{
		options.AppendExecutionProvider_CPU();
		return options;
	}
}
