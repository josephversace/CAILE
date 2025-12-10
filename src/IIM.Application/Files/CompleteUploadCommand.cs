using System;
using System.Collections.Generic;
using System.Text;
using IIM.Shared.Dtos;
using IIM.Shared.Mediator;

namespace IIM.Application.Files
{
	public record CompleteUploadCommand(
		Guid WorkspaceId,
		string SeaweedFileId,
		string OriginalFileName
	) : ICommand;


}
