using System;
using System.Collections.Generic;
using System.Text;
using IIM.Application.Users;
using IIM.Shared.Interfaces;
using IIM.Shared.Mediator;
using IIM.Shared.Models;

namespace IIM.Application
{
    public class GetProcessedFilesByVirtualFileIdHandler : IRequestHandler<GetProcessedFilesByVirtualFileId, List<IIM.Shared.Models.ProcessedFile>>
	{
		private readonly IWorkspaceManager _workspace;
		public GetProcessedFilesByVirtualFileIdHandler(IWorkspaceManager workspace)
		{
			_workspace = workspace;
		}

		public async Task<List<IIM.Shared.Models.ProcessedFile>> Handle(
			GetProcessedFilesByVirtualFileId request,
			CancellationToken cancellationToken)
		{
			if (string.IsNullOrWhiteSpace(request.ToString()))
				return  new List<IIM.Shared.Models.ProcessedFile>();

			var result = await _workspace.GetProcessedFilesAsync(request.VirtualFileId,cancellationToken);
			
			return result?.ToList() ?? new List<IIM.Shared.Models.ProcessedFile>();
		}

	}
}
