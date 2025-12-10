using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces
{
	public interface IHashService
	{
		Task<string> ComputeBlake3Async(Stream stream, CancellationToken ct = default);
		Task<string> ComputeBlake3Async(byte[] data, CancellationToken ct = default);

		Task<string> ComputeMd5Async(Stream stream, CancellationToken ct = default);

		Task<string> ComputeSha256Async(Stream stream, CancellationToken ct = default);
	}
}
