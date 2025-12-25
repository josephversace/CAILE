using System;
using System.Collections.Generic;
using System.Text;
using IIM.Shared.Mediator;


namespace IIM.Application
{
	public sealed record GetProcessedFilesByVirtualFileId(Guid VirtualFileId): IRequest<List<IIM.Shared.Models.ProcessedFile>>;
}
