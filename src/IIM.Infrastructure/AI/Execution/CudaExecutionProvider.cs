using Microsoft.ML.OnnxRuntime;

public class CudaExecutionProvider : IOnnxExecutionProvider
{
	public string GenAiName => "CUDAExecutionProvider";
	public string Name => "CUDA";

	public SessionOptions Configure(SessionOptions options)
	{
		options.AppendExecutionProvider_CUDA();
		return options;
	}
}
