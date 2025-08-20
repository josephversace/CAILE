// ============================================
// File: src/IIM.Infrastructure/Platform/WslManager.Core.cs
// Purpose: Core WSL operations and status management
// Author: IIM Platform Team
// Created: 2024
// ============================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using IIM.Infrastructure.Platform.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace IIM.Infrastructure.Platform
{
    /// <summary>
    /// Core WSL management operations
    /// </summary>
    public sealed partial class WslManager : IWslManager
    {
        private readonly ILogger<WslManager> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly HttpClient _httpClient;
        private readonly SemaphoreSlim _installLock = new(1, 1);
        private readonly string _configPath;

        // Configuration constants
        private const string UBUNTU_DISTRO = "IIM-Ubuntu";
        private const string KALI_DISTRO = "IIM-Kali";
        private const string WSL_KERNEL_URL = "https://aka.ms/wsl2kernel";
        private const int COMMAND_TIMEOUT_MS = 30000;
        private const int MAX_RETRIES = 3;
        private const int STARTUP_TIMEOUT_SECONDS = 120;

        // Service ports mapping
        private readonly Dictionary<string, int> _servicePorts = new()
        {
            ["qdrant"] = 6333,
            ["postgres"] = 5432,
            ["minio"] = 9000,
            ["minio-console"] = 9001,
            ["mcp-server"] = 3000
        };

        /// <summary>
        /// Initializes a new instance of the WslManager class.
        /// </summary>
        /// <param name="logger">Logger for diagnostic output</param>
        /// <param name="httpClientFactory">Factory for creating HTTP clients</param>
        public WslManager(ILogger<WslManager> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _httpClient = _httpClientFactory.CreateClient("wsl");

            // Set configuration path
            _configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "IIM",
                "WSL"
            );
            Directory.CreateDirectory(_configPath);

            _logger.LogInformation("WslManager initialized with config path: {ConfigPath}", _configPath);
        }

        /// <summary>
        /// Gets the current WSL status including installation state and available distributions.
        /// </summary>
        public async Task<WslStatus> GetStatusAsync(CancellationToken ct = default)
        {
            var status = new WslStatus
            {
                Timestamp = DateTimeOffset.UtcNow
            };

            try
            {
                // Check if WSL is installed
                status.IsInstalled = await CheckWslInstalledAsync(ct);

                if (!status.IsInstalled)
                {
                    status.Message = "WSL is not installed. Run 'wsl --install' as administrator.";
                    return status;
                }

                // Get WSL version info
                var versionInfo = await GetWslVersionInfoAsync(ct);
                status.Version = versionInfo.version;
                status.KernelVersion = versionInfo.kernel;
                status.IsWsl2 = versionInfo.isWsl2;

                // Check Windows features
                status.VirtualMachinePlatform = await CheckWindowsFeatureAsync("VirtualMachinePlatform", ct);
                status.HyperV = await CheckWindowsFeatureAsync("Microsoft-Hyper-V", ct);

                // Get installed distributions
                status.InstalledDistros = await GetInstalledDistrosAsync(ct);
                status.HasIimDistro = status.InstalledDistros.Any(d =>
                    d.Name == UBUNTU_DISTRO || d.Name == KALI_DISTRO);

                // Determine overall readiness
                status.IsReady = status.IsInstalled &&
                                status.IsWsl2 &&
                                status.HasIimDistro &&
                                status.InstalledDistros.Any(d => d.State == WslDistroState.Running);

                // Set appropriate message
                if (status.IsReady)
                {
                    status.Message = "WSL is ready for IIM operations.";
                }
                else if (!status.IsWsl2)
                {
                    status.Message = "WSL2 is required. Update WSL with 'wsl --update'.";
                }
                else if (!status.HasIimDistro)
                {
                    status.Message = "IIM distributions not found. Run setup to install.";
                }
                else
                {
                    status.Message = "WSL is installed but not fully configured for IIM.";
                }

                _logger.LogInformation("WSL status check complete: {Message}", status.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking WSL status");
                status.Message = $"Error checking WSL status: {ex.Message}";
            }

            return status;
        }

        /// <summary>
        /// Checks if WSL is enabled on the system.
        /// </summary>
        public async Task<bool> IsWslEnabled()
        {
            try
            {
                var result = await RunCommandAsync("wsl", "--status", 5000);
                return result.ExitCode == 0;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "WSL not enabled or not installed");
                return false;
            }
        }

        /// <summary>
        /// Enables WSL on the system. Requires administrator privileges.
        /// </summary>
        public async Task<bool> EnableWsl()
        {
            await _installLock.WaitAsync();
            try
            {
                _logger.LogInformation("Attempting to enable WSL");

                // Check if running as administrator
                if (!IsRunningAsAdministrator())
                {
                    _logger.LogWarning("Not running as administrator. Generating enablement script.");
                    await GenerateWslEnablementScriptAsync();
                    return false;
                }

                // Enable WSL feature
                var wslResult = await RunCommandAsync("dism.exe",
                    "/online /enable-feature /featurename:Microsoft-Windows-Subsystem-Linux /all /norestart",
                    60000);

                if (wslResult.ExitCode != 0)
                {
                    _logger.LogError("Failed to enable WSL feature: {Error}", wslResult.StandardError);
                    return false;
                }

                // Enable Virtual Machine Platform
                var vmResult = await RunCommandAsync("dism.exe",
                    "/online /enable-feature /featurename:VirtualMachinePlatform /all /norestart",
                    60000);

                if (vmResult.ExitCode != 0)
                {
                    _logger.LogError("Failed to enable Virtual Machine Platform: {Error}", vmResult.StandardError);
                    return false;
                }

                // Set WSL2 as default
                await RunCommandAsync("wsl", "--set-default-version 2", 5000);

                _logger.LogInformation("WSL enabled successfully. Restart required.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enable WSL");
                return false;
            }
            finally
            {
                _installLock.Release();
            }
        }

        /// <summary>
        /// Executes a command in the default WSL distribution.
        /// </summary>
        public async Task<CommandResult> ExecuteCommandAsync(string command, CancellationToken ct = default)
        {
            return await ExecuteCommandAsync(UBUNTU_DISTRO, command, ct);
        }

        /// <summary>
        /// Executes a command in a specific WSL distribution.
        /// </summary>
        public async Task<CommandResult> ExecuteCommandAsync(string distroName, string command, CancellationToken ct = default)
        {
            return await ExecuteInDistroAsync(distroName, command, ct);
        }

        /// <summary>
        /// Executes a command with streaming output.
        /// </summary>
        public async Task<int> ExecuteCommandWithStreamingAsync(
            string distroName,
            string command,
            Action<string> outputCallback,
            CancellationToken ct = default)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "wsl",
                Arguments = $"-d {distroName} -u root -- bash -c \"{command.Replace("\"", "\\\"")}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    outputCallback(e.Data);
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    outputCallback($"ERROR: {e.Data}");
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(ct);
            return process.ExitCode;
        }

        // ========================================
        // Private Helper Methods
        // ========================================

        /// <summary>
        /// Checks if WSL is installed by attempting to run the wsl command.
        /// </summary>
        private async Task<bool> CheckWslInstalledAsync(CancellationToken ct)
        {
            try
            {
                var result = await RunCommandAsync("wsl", "--version", 5000, ct);
                return result.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets WSL version information.
        /// </summary>
        private async Task<(string version, string kernel, bool isWsl2)> GetWslVersionInfoAsync(CancellationToken ct)
        {
            try
            {
                var result = await RunCommandAsync("wsl", "--version", 5000, ct);
                if (result.ExitCode == 0)
                {
                    var output = result.StandardOutput;

                    // Parse version info from output
                    var versionMatch = Regex.Match(output, @"WSL version: ([\d\.]+)");
                    var kernelMatch = Regex.Match(output, @"Kernel version: ([\d\.\-]+)");

                    return (
                        versionMatch.Success ? versionMatch.Groups[1].Value : "Unknown",
                        kernelMatch.Success ? kernelMatch.Groups[1].Value : "Unknown",
                        true // If --version works, it's WSL2
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not get WSL version info");
            }

            return ("Unknown", "Unknown", false);
        }

        /// <summary>
        /// Checks if a Windows feature is enabled.
        /// </summary>
        private async Task<bool> CheckWindowsFeatureAsync(string featureName, CancellationToken ct)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    using var key = Registry.LocalMachine.OpenSubKey(
                        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\Packages");

                    if (key != null)
                    {
                        var featureKeys = key.GetSubKeyNames()
                            .Where(k => k.Contains(featureName, StringComparison.OrdinalIgnoreCase));

                        return featureKeys.Any();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not check Windows feature {Feature}", featureName);
            }

            return false;
        }

        /// <summary>
        /// Gets list of installed WSL distributions.
        /// </summary>
        private async Task<List<WslDistro>> GetInstalledDistrosAsync(CancellationToken ct)
        {
            var distros = new List<WslDistro>();

            try
            {
                var result = await RunCommandAsync("wsl", "--list --verbose", 5000, ct);
                if (result.ExitCode == 0)
                {
                    var lines = result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                    // Skip header line
                    foreach (var line in lines.Skip(1))
                    {
                        var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 3)
                        {
                            var name = parts[0].Trim('*', ' ');
                            var state = parts[1].ToLower() switch
                            {
                                "running" => WslDistroState.Running,
                                "stopped" => WslDistroState.Stopped,
                                _ => WslDistroState.Unknown
                            };
                            var version = parts[2];

                            distros.Add(new WslDistro
                            {
                                Name = name,
                                State = state,
                                Version = version,
                                IsDefault = line.StartsWith('*')
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get installed distros");
            }

            return distros;
        }

        /// <summary>
        /// Checks if running as administrator.
        /// </summary>
        private bool IsRunningAsAdministrator()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            return false;
        }

        /// <summary>
        /// Generates a PowerShell script for WSL enablement.
        /// </summary>
        private async Task GenerateWslEnablementScriptAsync()
        {
            var scriptPath = Path.Combine(_configPath, "enable-wsl.ps1");
            var script = @"
# IIM WSL Enablement Script
# Run this script as Administrator

Write-Host 'Enabling WSL and required features...' -ForegroundColor Green

# Enable WSL
dism.exe /online /enable-feature /featurename:Microsoft-Windows-Subsystem-Linux /all /norestart

# Enable Virtual Machine Platform
dism.exe /online /enable-feature /featurename:VirtualMachinePlatform /all /norestart

# Download and install WSL2 kernel
Write-Host 'Downloading WSL2 kernel...' -ForegroundColor Yellow
Invoke-WebRequest -Uri https://aka.ms/wsl2kernel -OutFile wsl_update_x64.msi
Start-Process msiexec.exe -Wait -ArgumentList '/I wsl_update_x64.msi /quiet'

# Set WSL2 as default
wsl --set-default-version 2

Write-Host 'WSL enabled successfully. Please restart your computer.' -ForegroundColor Green
Read-Host 'Press Enter to continue'
";
            await File.WriteAllTextAsync(scriptPath, script);
            _logger.LogInformation("WSL enablement script generated at: {Path}", scriptPath);
        }

        /// <summary>
        /// Runs a command and returns the result.
        /// </summary>
        private async Task<CommandResult> RunCommandAsync(
            string fileName,
            string arguments,
            int timeoutMs = COMMAND_TIMEOUT_MS,
            CancellationToken ct = default)
        {
            var stopwatch = Stopwatch.StartNew();

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                    outputBuilder.AppendLine(e.Data);
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                    errorBuilder.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeoutMs);

            try
            {
                await process.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Command timed out: {Command} {Arguments}", fileName, arguments);
                process.Kill();
                throw;
            }

            stopwatch.Stop();

            return new CommandResult
            {
                ExitCode = process.ExitCode,
                StandardOutput = outputBuilder.ToString(),
                StandardError = errorBuilder.ToString(),
                ExecutionTime = stopwatch.Elapsed
            };
        }

        /// <summary>
        /// Executes a command in a specific WSL distro.
        /// </summary>
        private async Task<CommandResult> ExecuteInDistroAsync(
            string distro,
            string command,
            CancellationToken ct)
        {
            return await RunCommandAsync("wsl",
                $"-d {distro} -u root -- bash -c \"{command.Replace("\"", "\\\"")}\"",
                COMMAND_TIMEOUT_MS, ct);
        }
    }
}