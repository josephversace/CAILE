using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Interfaces;
using IIM.Shared.DTOs;
using Microsoft.Extensions.Logging;

namespace IIM.Core.Plugins.Security.Implementations;

public class SandboxedProcessRunner : ISecureProcessRunner
{
    private readonly ILogger<SandboxedProcessRunner> _logger;

    public SandboxedProcessRunner(ILogger<SandboxedProcessRunner> logger)
    {
        _logger = logger;
    }

    public async Task<ProcessResult> RunAsync(string command, string[] args, CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = string.Join(" ", args),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            return new ProcessResult
            {
                ExitCode = -1,
                StandardError = "Failed to start process"
            };
        }

        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var error = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        return new ProcessResult
        {
            ExitCode = process.ExitCode,
            StandardOutput = output,
            StandardError = error
        };
    }
}
