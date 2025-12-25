using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Models;
using IIM.Shared.Models.Core;

namespace IIM.Shared.Interfaces
{
	public interface IProcessedFileWriter
	{
		Task<ProcessedFile> WriteAsync(
			ProcessedFileWriteRequest request,
			CancellationToken ct = default);
	}

}
