using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.DTOs;
using IIM.Shared.Models.Audit;

namespace IIM.Shared.Interfaces;

public interface ISecureProcessRunner
{
    Task<ProcessResult> RunAsync(string command, string[] args, CancellationToken cancellationToken = default);
}
