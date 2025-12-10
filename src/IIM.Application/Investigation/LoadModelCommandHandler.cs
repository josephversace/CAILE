using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Mediator;
using IIM.Shared.Models;

namespace IIM.Application.Investigation
{
	/// <summary>
	/// Temporary stub that replaces the legacy model-loading orchestration.
	/// This lets the API compile and run until the new Foundry/ONNX pipeline is wired in.
	/// </summary>
	public class LoadModelCommandHandler
		: IRequestHandler<LoadModelCommand, ModelHandle>
	{
		public Task<ModelHandle> Handle(
			LoadModelCommand request,
			CancellationToken cancellationToken)
		{
			// TODO: Replace with real local-model loading
			// using Foundry Local or your ONNX Runtime orchestration service.

			var handle = new ModelHandle
			{
				ModelId = request.ModelId
			
			};

			return Task.FromResult(handle);
		}
	}
}
