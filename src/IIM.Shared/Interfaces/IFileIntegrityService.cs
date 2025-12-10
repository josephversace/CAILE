using System;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces
{
	public interface IFileIntegrityService
	{
		Task<bool> VerifyAsync(Guid virtualFileId, CancellationToken ct = default);
	}
}
