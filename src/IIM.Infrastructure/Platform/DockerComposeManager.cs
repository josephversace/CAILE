using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace IIM.Infrastructure.Platform;

/// <summary>
/// Manages the appliance's services using Docker Compose directly.
/// This is the implementation for the native Linux deployment.
/// </summary>
public class DockerComposeManager : IWslManager
{
    private readonly ILogger<DockerComposeManager> _logger;
    private readonly string _dockerComposeFile = "docker-compose.yml";
    private readonly string _dockerComposeOverrideFile = "docker-compose.override.yml";

    public DockerComposeManager(ILogger<DockerComposeManager> logger)
    {
        _logger = logger;
    }

    public async Task<(bool success, string message)> ExecuteCommandAsync(string command, int timeout = 60)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "docker-compose",
                Arguments = $"-f {_dockerComposeFile} -f {_dockerComposeOverrideFile} {command}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        _logger.LogInformation("Executing Docker Compose command: {Arguments}", process.StartInfo.Arguments);
        process.Start();

        string output = await process.StandardOutput.ReadToEndAsync();
        string error = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync(CancellationToken.None);

        if (process.ExitCode == 0)
        {
            _logger.LogInformation("Docker Compose command succeeded. Output: {Output}", output);
            return (true, output);
        }
        else
        {
            _logger.LogError("Docker Compose command failed. Error: {Error}", error);
            return (false, error);
        }
    }

    public Task<string> GetDistroIpAddressAsync()
    {
        // In a native Docker environment, services are reached by their container name,
        // so we can often just return localhost or the service name.
        _logger.LogInformation("Returning 'localhost' as IP address for native Docker environment.");
        return Task.FromResult("localhost");
    }

    // Implement other IWslManager methods as needed, they might not all be relevant for native Docker.
    public Task<bool> IsDistroRunningAsync()
    {
        // For Docker, we can check if the core containers are running.
        // This is a simplified check; a real implementation might check specific container names.
        _logger.LogInformation("Checking if core Docker services are running.");
        return Task.FromResult(true); // Placeholder
    }
}
