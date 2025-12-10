using IIM.Infrastructure.AI.DirectML;
using IIM.Shared.Interfaces;
using Microsoft.ML.OnnxRuntime;

namespace IIM.Infrastructure.AI.Execution
{
	public class DirectMLExecutionProvider : IOnnxExecutionProvider
	{
		private readonly IDirectMLDeviceManager _deviceManager;

		// Used by GenAI
		public string GenAiName => "DmlExecutionProvider";

		// Used by your diagnostics/UI
		public string Name => "DirectML";

		public DirectMLExecutionProvider(IDirectMLDeviceManager deviceManager)
		{
			_deviceManager = deviceManager
				?? throw new ArgumentNullException(nameof(deviceManager));
		}

		public SessionOptions Configure(SessionOptions options)
		{
			// Classic ORT path:
			// Use the DeviceManager to produce a fully configured SessionOptions.
			return _deviceManager.GetSessionOptions(deviceId: 0);
		}
	}
}
