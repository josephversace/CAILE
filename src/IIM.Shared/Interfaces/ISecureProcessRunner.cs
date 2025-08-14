using System.Threading.Tasks;
using System.Threading;
using IIM.Shared.Models;

namespace IIM.Shared.Interfaces
{
    public interface ISecureProcessRunner
    {
        Task<ProcessResult> RunAsync(string fileName, string arguments);

        Task<ProcessResult> RunAsync(string tool, string[] args, CancellationToken ct = default);

    }
}
