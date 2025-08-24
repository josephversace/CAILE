using System.Threading;
using System.Threading.Tasks;

using IIM.Shared.Models;

namespace IIM.Shared.Interfaces;

public interface ISecureProcessRunner
{
    Task<ProcessResult> RunAsync(string command, string[] args, CancellationToken cancellationToken = default);
}
