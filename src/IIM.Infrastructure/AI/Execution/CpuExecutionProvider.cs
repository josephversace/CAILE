using Microsoft.ML.OnnxRuntime;

public class CpuExecutionProvider : IOnnxExecutionProvider
{
	public string GenAiName => "CPUExecutionProvider";
	public string Name => "CPU";

	public SessionOptions Configure(SessionOptions options)
	{
		options.AppendExecutionProvider_CPU();
		return options;
	}
}
