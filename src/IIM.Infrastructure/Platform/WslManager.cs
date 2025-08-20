// ============================================
// File: src/IIM.Infrastructure/Platform/WslManager.cs
// Purpose: Complete WSL2 management with DirectML support for AMD Strix
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
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace IIM.Infrastructure.Platform
{
    /// <summary>
    /// Manages WSL2 installation, configuration, and health monitoring for IIM platform.
    /// Provides fallback paths for non-admin users and supports both Ubuntu (services) and Kali (forensics).
    /// </summary>
    public sealed class WslManager : IWslManager
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
            ["mcp-server"] = 3000  // MCP server for Kali tools
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

        #region Public Interface Implementation

        /// <summary>
        /// Installs Tor (if missing) and applies proxy environment variables for WSL processes.
        /// Expects proxy.env to be written on Windows and mapped into WSL.
        /// </summary>
        /// <param name="windowsProxyPath">Full Windows path to the proxy.env file (e.g. C:\Temp\proxy.env)</param>
        public async Task InstallTorAndApplyProxyAsync(string windowsProxyPath, CancellationToken cancellationToken)
        {
            // Convert Windows path to WSL format (e.g., C:\Temp\proxy.env -> /mnt/c/Temp/proxy.env)
            string wslProxyPath = $"/mnt/c{windowsProxyPath.Replace(":", "").Replace('\\', '/')}";

            // Bash script sets proxy env vars and ensures Tor is installed and running
            string bashScript = $@"
set -e
if [ -f ""{wslProxyPath}"" ]; then
  export $(cat ""{wslProxyPath}"" | xargs)
fi
if ! command -v tor > /dev/null; then
  sudo apt update && sudo apt install tor -y
fi
sudo systemctl enable tor
sudo systemctl start tor
env | grep -i proxy
";

            var psi = new ProcessStartInfo
            {
                FileName = "wsl.exe",
                Arguments = $"-e bash -c \"{bashScript.Replace("\"", "\\\"")}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            try
            {
                using var proc = Process.Start(psi);
                string output = await proc.StandardOutput.ReadToEndAsync();
                string error = await proc.StandardError.ReadToEndAsync();
                await proc.WaitForExitAsync(cancellationToken);

                if (proc.ExitCode != 0)
                {
                    _logger.LogError("WSL/Tor setup failed: {Error}", error);
                    throw new Exception("WSL/Tor setup failed:\n" + error);
                }
                _logger.LogInformation("WSL/Tor setup complete: {Output}", output);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during WSL/Tor setup");
                throw;
            }
        }


        /// <summary>
        /// Gets comprehensive WSL status including feature state and distro availability.
        /// Works without admin privileges by checking registry and running WSL commands.
        /// </summary>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Complete WSL status information</returns>
        public async Task<WslStatus> GetStatusAsync(CancellationToken ct = default)
        {
            var status = new WslStatus
            {
                Timestamp = DateTimeOffset.UtcNow,
                InstalledDistros = new List<WslDistro>()
            };

            try
            {
                _logger.LogDebug("Checking WSL status...");

                // Check if WSL is installed (works without admin)
                status.IsInstalled = await CheckWslInstalledAsync(ct);

                if (!status.IsInstalled)
                {
                    status.Message = "WSL is not installed. Admin privileges required for installation.";
                    _logger.LogWarning("WSL not installed");
                    return status;
                }

                // Check WSL version
                var versionInfo = await GetWslVersionAsync(ct);
                status.Version = versionInfo.Version;
                status.KernelVersion = versionInfo.KernelVersion;
                status.IsWsl2 = versionInfo.IsWsl2;

                // Check required Windows features
                status.VirtualMachinePlatform = await CheckWindowsFeatureAsync("VirtualMachinePlatform", ct);
                status.HyperV = await CheckWindowsFeatureAsync("Microsoft-Hyper-V", ct);

                // Get list of installed distros
                var distros = await GetInstalledDistrosAsync(ct);
                status.InstalledDistros = distros;

                // Check for our specific distros
                status.HasIimDistro = distros.Any(d =>
                    d.Name == UBUNTU_DISTRO || d.Name == KALI_DISTRO);

                // Determine overall readiness
                status.IsReady = status.IsInstalled && status.IsWsl2 && status.HasIimDistro;

                // Set appropriate status message
                if (status.IsReady)
                {
                    status.Message = "WSL2 is configured and ready for IIM platform";
                    _logger.LogInformation("WSL2 ready with IIM distros");
                }
                else if (!status.IsWsl2)
                {
                    status.Message = "WSL1 detected. WSL2 upgrade recommended for DirectML support.";
                    _logger.LogWarning("WSL1 detected, WSL2 required for optimal performance");
                }
                else if (!status.HasIimDistro)
                {
                    status.Message = $"WSL2 is ready but {UBUNTU_DISTRO} distro not found.";
                    _logger.LogWarning("IIM distros not installed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting WSL status");
                status.Message = $"Error checking WSL status: {ex.Message}";
                status.IsReady = false;
            }

            return status;
        }

        /// <summary>
        /// Checks if WSL is enabled on the system.
        /// </summary>
        /// <returns>True if WSL is enabled, false otherwise</returns>
        public async Task<bool> IsWslEnabled()
        {
            try
            {
                return await CheckWslInstalledAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if WSL is enabled");
                return false;
            }
        }

        /// <summary>
        /// Attempts to enable WSL with admin privileges.
        /// Falls back to manual instructions if admin rights are not available.
        /// </summary>
        /// <returns>True if WSL was successfully enabled, false otherwise</returns>
        public async Task<bool> EnableWsl()
        {
            await _installLock.WaitAsync();
            try
            {
                _logger.LogInformation("Attempting to enable WSL2");

                // Check if we have admin privileges
                bool isAdmin = IsRunningAsAdministrator();

                if (!isAdmin)
                {
                    _logger.LogWarning("Not running as administrator. Attempting elevation...");

                    // Try to restart with elevation
                    var elevated = await TryElevateAndEnableAsync();
                    if (!elevated)
                    {
                        // Provide manual instructions
                        await ShowManualInstallInstructionsAsync();
                        return false;
                    }
                    return true;
                }

                // We have admin rights, proceed with installation
                var steps = new List<(string Name, Func<Task<bool>> Action)>
                {
                    ("Enable WSL Feature", EnableWslFeatureAsync),
                    ("Enable Virtual Machine Platform", EnableVirtualMachinePlatformAsync),
                    ("Install WSL2 Kernel", InstallWsl2KernelAsync),
                    ("Set WSL2 as Default", SetWsl2AsDefaultAsync)
                };

                foreach (var (name, action) in steps)
                {
                    _logger.LogInformation("Executing step: {StepName}", name);
                    if (!await action())
                    {
                        _logger.LogError("WSL installation step failed: {StepName}", name);
                        return false;
                    }
                }

                _logger.LogInformation("WSL2 successfully enabled");
                return true;
            }
            finally
            {
                _installLock.Release();
            }
        }

        /// <summary>
        /// Ensures the specified distro is installed and configured.
        /// Supports Ubuntu for services and Kali for forensic tools.
        /// </summary>
        /// <param name="distroName">Name of the distro to ensure (default: IIM-Ubuntu)</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>The ensured WSL distro</returns>
        public async Task<WslDistro> EnsureDistroAsync(string distroName = "IIM-Ubuntu", CancellationToken ct = default)
        {
            _logger.LogInformation("Ensuring distro {DistroName}", distroName);

            // Check if distro already exists
            var existing = await GetDistroAsync(distroName, ct);
            if (existing != null && existing.State == WslDistroState.Running)
            {
                _logger.LogDebug("Distro {DistroName} already running", distroName);
                return existing;
            }

            // Install distro if not present
            if (existing == null)
            {
                _logger.LogInformation("Installing {Distro} distro", distroName);

                var success = distroName switch
                {
                    UBUNTU_DISTRO => await InstallUbuntuDistroAsync(ct),
                    KALI_DISTRO => await InstallKaliDistroAsync(ct),
                    _ => throw new NotSupportedException($"Distro {distroName} not supported")
                };

                if (!success)
                {
                    throw new InvalidOperationException($"Failed to install {distroName}");
                }

                existing = await GetDistroAsync(distroName, ct);
            }

            // Start distro if stopped
            if (existing?.State == WslDistroState.Stopped)
            {
                _logger.LogInformation("Starting distro {DistroName}", distroName);
                await StartDistroAsync(distroName, ct);
                existing = await GetDistroAsync(distroName, ct);
            }

            // Configure distro on first run
            if (existing != null && !await IsDistroConfiguredAsync(distroName, ct))
            {
                _logger.LogInformation("Configuring distro {DistroName}", distroName);
                await ConfigureDistroAsync(distroName, ct);
            }

            return existing ?? throw new InvalidOperationException($"Failed to ensure {distroName}");
        }

        /// <summary>
        /// Gets network information for a WSL distro including IP addresses and service endpoints.
        /// </summary>
        /// <param name="distroName">Name of the distro</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Network information for the distro</returns>
        public async Task<WslNetworkInfo> GetNetworkInfoAsync(string distroName, CancellationToken ct = default)
        {
            var networkInfo = new WslNetworkInfo
            {
                DistroName = distroName,
                ServiceEndpoints = new Dictionary<string, string>()
            };

            try
            {
                // Get WSL IP address
                var ipOutput = await RunWslCommandAsync(distroName,
                    "ip addr show eth0 | grep 'inet ' | awk '{print $2}' | cut -d/ -f1", ct);

                networkInfo.WslIpAddress = ipOutput.Trim();

                // Get Windows host IP from WSL perspective
                var hostOutput = await RunWslCommandAsync(distroName,
                    "ip route | grep default | awk '{print $3}'", ct);

                networkInfo.WindowsHostIp = hostOutput.Trim();

                // Build service endpoints
                if (!string.IsNullOrEmpty(networkInfo.WslIpAddress))
                {
                    foreach (var service in _servicePorts)
                    {
                        networkInfo.ServiceEndpoints[service.Key] =
                            $"http://{networkInfo.WslIpAddress}:{service.Value}";
                    }
                    networkInfo.IsConnected = true;
                }

                // Test connectivity to Qdrant
                if (!string.IsNullOrEmpty(networkInfo.WslIpAddress))
                {
                    using var client = _httpClientFactory.CreateClient();
                    client.Timeout = TimeSpan.FromSeconds(2);

                    try
                    {
                        var response = await client.GetAsync(
                            $"http://{networkInfo.WslIpAddress}:6333/", ct);
                        // Don't need success, just that it connects
                    }
                    catch
                    {
                        _logger.LogDebug("Qdrant not yet reachable at {IP}", networkInfo.WslIpAddress);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get network info for {Distro}", distroName);
                networkInfo.ErrorMessage = ex.Message;
                networkInfo.IsConnected = false;
            }

            return networkInfo;
        }

        /// <summary>
        /// Performs comprehensive health check of WSL and required services.
        /// </summary>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Health check result with detailed issues if any</returns>
        public async Task<HealthCheckResult> HealthCheckAsync(CancellationToken ct = default)
        {
            var result = new HealthCheckResult
            {
                Timestamp = DateTimeOffset.UtcNow,
                IsHealthy = true,
                Issues = new List<string>()
            };

            try
            {
                _logger.LogDebug("Performing WSL health check");

                // Check WSL status
                var status = await GetStatusAsync(ct);
                result.WslReady = status.IsReady;

                if (!status.IsInstalled)
                {
                    result.IsHealthy = false;
                    result.Issues = result.Issues.Append("WSL is not installed").ToList();
                    return result;
                }

                if (!status.IsWsl2)
                {
                    result.Issues = result.Issues.Append("WSL1 detected, WSL2 recommended for DirectML").ToList();
                }

                // Check Ubuntu distro
                var ubuntu = await GetDistroAsync(UBUNTU_DISTRO, ct);
                result.DistroRunning = ubuntu?.State == WslDistroState.Running;

                if (ubuntu == null)
                {
                    result.IsHealthy = false;
                    result.Issues = result.Issues.Append($"{UBUNTU_DISTRO} distro not found").ToList();
                }
                else if (ubuntu.State != WslDistroState.Running)
                {
                    result.Issues = result.Issues.Append($"{UBUNTU_DISTRO} is not running").ToList();
                }

                // Check Docker inside WSL
                if (ubuntu?.State == WslDistroState.Running)
                {
                    try
                    {
                        var dockerVersion = await RunWslCommandAsync(UBUNTU_DISTRO, "docker --version", ct);
                        if (!dockerVersion.Contains("Docker version"))
                        {
                            result.Issues = result.Issues.Append("Docker not found in WSL").ToList();
                        }
                        else
                        {
                            result.ServicesHealthy = true;
                        }
                    }
                    catch
                    {
                        result.Issues = result.Issues.Append("Cannot execute Docker in WSL").ToList();
                        result.ServicesHealthy = false;
                    }
                }

                // Check network connectivity
                if (ubuntu?.State == WslDistroState.Running)
                {
                    var network = await GetNetworkInfoAsync(UBUNTU_DISTRO, ct);
                    result.NetworkConnected = network.IsConnected;

                    if (!network.IsConnected)
                    {
                        result.Issues = result.Issues.Append("WSL network not connected").ToList();
                    }
                }

                // Check memory availability
                var memInfo = await GetWslMemoryInfoAsync(ct);
                if (memInfo.AvailableGb < 4)
                {
                    result.Issues = result.Issues.Append(
                        $"Low memory: {memInfo.AvailableGb:F1}GB available (4GB recommended)").ToList();
                }

                result.IsHealthy = !result.Issues.Any();

                _logger.LogInformation("Health check completed. Healthy: {IsHealthy}, Issues: {IssueCount}",
                    result.IsHealthy, result.Issues.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check error");
                result.IsHealthy = false;
                result.Issues = result.Issues.Append($"Health check error: {ex.Message}").ToList();
            }

            return result;
        }

        /// <summary>
        /// Checks if a specific distro exists.
        /// </summary>
        /// <param name="distroName">Name of the distro to check</param>
        /// <returns>True if the distro exists, false otherwise</returns>
        public async Task<bool> DistroExists(string distroName = "IIM-Ubuntu")
        {
            try
            {
                var distros = await GetInstalledDistrosAsync();
                return distros.Any(d => d.Name.Equals(distroName, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if distro {DistroName} exists", distroName);
                return false;
            }
        }

        /// <summary>
        /// Starts all IIM services in WSL.
        /// </summary>
        /// <returns>True if services started successfully</returns>
        public async Task<bool> StartIim()
        {
            try
            {
                _logger.LogInformation("Starting IIM services");

                // Ensure Ubuntu distro is running
                var ubuntu = await EnsureDistroAsync(UBUNTU_DISTRO);

                // Start services
                return await StartServicesAsync(ubuntu);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start IIM services");
                return false;
            }
        }

        /// <summary>
        /// Starts all required services in the WSL distribution.
        /// </summary>
        /// <param name="distro">The WSL distro to start services in</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>True if services started successfully</returns>
        public async Task<bool> StartServicesAsync(WslDistro distro, CancellationToken ct = default)
        {
            _logger.LogInformation("Starting services in {Distro}", distro.Name);

            try
            {
                // Generate and execute startup script
                var startupScript = GenerateStartupScript();
                var scriptPath = "/tmp/start_iim_services.sh";

                // Copy script to WSL
                await ExecuteInDistroAsync(distro.Name,
                    $"cat > {scriptPath} << 'EOF'\n{startupScript}\nEOF", ct);

                // Make executable and run
                await ExecuteInDistroAsync(distro.Name, $"chmod +x {scriptPath}", ct);
                await ExecuteInDistroAsync(distro.Name,
                    $"nohup bash {scriptPath} > /var/log/iim_services.log 2>&1 &", ct);

                // Wait for services to start
                await Task.Delay(5000, ct);

                // Verify health
                var health = await CheckServicesHealthAsync(distro, ct);

                if (!health.IsHealthy)
                {
                    _logger.LogWarning("Some services failed to start: {Details}", health.Details);

                    // Attempt to fix issues
                    await DiagnoseAndFixServicesAsync(distro, health, ct);

                    // Re-check health
                    health = await CheckServicesHealthAsync(distro, ct);
                }

                return health.IsHealthy;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start services in {Distro}", distro.Name);
                return false;
            }
        }

        /// <summary>
        /// Syncs files between Windows and WSL with incremental support.
        /// </summary>
        /// <param name="windowsPath">Windows source path</param>
        /// <param name="wslPath">WSL destination path</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>True if sync was successful</returns>
        public async Task<bool> SyncFilesAsync(string windowsPath, string wslPath, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Syncing files from {WindowsPath} to {WslPath}", windowsPath, wslPath);

                // Convert Windows path to WSL path
                var wslWindowsPath = windowsPath.Replace('\\', '/').Replace("C:", "/mnt/c");

                // Use rsync for incremental sync
                var command = $"rsync -av --progress {wslWindowsPath}/ {wslPath}/";
                var result = await RunWslCommandAsync(UBUNTU_DISTRO, command, ct);

                _logger.LogDebug("Sync result: {Result}", result);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync files");
                return false;
            }
        }

        /// <summary>
        /// Installs a WSL distro from a tar.gz file.
        /// </summary>
        /// <param name="distroPath">Path to the distro tar.gz file</param>
        /// <param name="installName">Name for the installed distro</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>True if installation was successful</returns>
        public async Task<bool> InstallDistroAsync(string distroPath, string installName, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Installing distro {Name} from {Path}", installName, distroPath);

                var installPath = Path.Combine(_configPath, installName);
                Directory.CreateDirectory(installPath);

                var result = await RunCommandAsync("wsl",
                    $"--import {installName} \"{installPath}\" \"{distroPath}\" --version 2",
                    60000, ct);

                return result.ExitCode == 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to install distro {Name}", installName);
                return false;
            }
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Checks if WSL is installed by attempting to run the wsl command.
        /// </summary>
        private async Task<bool> CheckWslInstalledAsync(CancellationToken ct = default)
        {
            try
            {
                var result = await RunCommandAsync("wsl", "--version", COMMAND_TIMEOUT_MS, ct);
                return result.StandardOutput.Contains("WSL version") ||
                       result.StandardOutput.Contains("Windows Subsystem for Linux");
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets WSL version information.
        /// </summary>
        private async Task<(string Version, string KernelVersion, bool IsWsl2)> GetWslVersionAsync(CancellationToken ct = default)
        {
            try
            {
                var result = await RunCommandAsync("wsl", "--version", COMMAND_TIMEOUT_MS, ct);

                var versionMatch = Regex.Match(result.StandardOutput, @"WSL version:\s*([\d\.]+)");
                var kernelMatch = Regex.Match(result.StandardOutput, @"Kernel version:\s*([\d\.\-]+)");

                var version = versionMatch.Success ? versionMatch.Groups[1].Value : "unknown";
                var kernel = kernelMatch.Success ? kernelMatch.Groups[1].Value : "unknown";
                var isWsl2 = version.StartsWith("2") || result.StandardOutput.Contains("WSL 2");

                return (version, kernel, isWsl2);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get WSL version");
                return ("unknown", "unknown", false);
            }
        }

        /// <summary>
        /// Checks if a Windows feature is enabled using registry or DISM.
        /// </summary>
        private async Task<bool> CheckWindowsFeatureAsync(string featureName, CancellationToken ct = default)
        {
            try
            {
                // Try registry first (no admin required)
                using var key = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\Notifications\OptionalFeatures\" + featureName);

                if (key != null)
                {
                    var status = key.GetValue("Status")?.ToString();
                    return status == "0"; // 0 means enabled
                }

                // Fallback to DISM (might need admin)
                var result = await RunCommandAsync("dism",
                    $"/online /get-featureinfo /featurename:{featureName}", 5000, ct);

                return result.StandardOutput.Contains("State : Enabled");
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets list of installed WSL distros.
        /// </summary>
        private async Task<List<WslDistro>> GetInstalledDistrosAsync(CancellationToken ct = default)
        {
            var distros = new List<WslDistro>();

            try
            {
                var result = await RunCommandAsync("wsl", "--list --verbose", COMMAND_TIMEOUT_MS, ct);
                var lines = result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                // Parse each line (skip header)
                foreach (var line in lines.Skip(1))
                {
                    var cleanLine = Regex.Replace(line, @"[^\x20-\x7E]", ""); // Remove non-ASCII
                    var parts = cleanLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length >= 3)
                    {
                        var distro = new WslDistro
                        {
                            Name = parts[0].TrimStart('*').Trim(),
                            State = parts[1].ToLower() switch
                            {
                                "running" => WslDistroState.Running,
                                "stopped" => WslDistroState.Stopped,
                                _ => WslDistroState.Unknown
                            },
                            Version = parts[2]
                        };
                        distros.Add(distro);
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
        /// Gets a specific distro by name.
        /// </summary>
        private async Task<WslDistro?> GetDistroAsync(string name, CancellationToken ct = default)
        {
            var distros = await GetInstalledDistrosAsync(ct);
            return distros.FirstOrDefault(d => d.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Checks if running with administrator privileges.
        /// </summary>
        private bool IsRunningAsAdministrator()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return false;

            try
            {
                using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Attempts to elevate privileges and enable WSL.
        /// </summary>
        private async Task<bool> TryElevateAndEnableAsync()
        {
            try
            {
                var scriptPath = Path.Combine(Path.GetTempPath(), "enable-wsl-iim.ps1");
                await File.WriteAllTextAsync(scriptPath, GetWslInstallScript());

                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\"",
                    UseShellExecute = true,
                    Verb = "runas" // Request elevation
                };

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    return process.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to elevate and enable WSL");
            }

            return false;
        }

        /// <summary>
        /// Shows manual installation instructions for users without admin rights.
        /// </summary>
        private async Task ShowManualInstallInstructionsAsync()
        {
            var instructions = @"
# WSL2 Manual Installation Instructions for IIM Platform

WSL2 requires administrator privileges to install. Please follow these steps:

## Option 1: Request IT Support
1. Send the generated script 'IIM_WSL_Install_Script.ps1' to your IT administrator
2. They can run it with admin privileges to set up WSL2

## Option 2: Manual Installation
1. Open PowerShell as Administrator (Right-click > Run as Administrator)

2. Run these commands in order:

   # Enable WSL feature
   dism.exe /online /enable-feature /featurename:Microsoft-Windows-Subsystem-Linux /all /norestart

   # Enable Virtual Machine Platform
   dism.exe /online /enable-feature /featurename:VirtualMachinePlatform /all /norestart

3. Restart your computer

4. After restart, download and install the WSL2 kernel:
   https://aka.ms/wsl2kernel

5. Set WSL2 as default:
   wsl --set-default-version 2

6. Return to IIM and click 'Check Again'

## Option 3: Portable Mode
If WSL cannot be installed, IIM can run in portable mode with limited features:
- DirectML inference will work for ONNX models
- LlamaSharp will work for LLM inference
- RAG features will be limited without Qdrant
- No Kali forensic tools integration

Files have been saved to your desktop:
- IIM_WSL_Install_Instructions.txt
- IIM_WSL_Install_Script.ps1
";

            _logger.LogInformation("Showing manual install instructions");

            // Save instructions to file
            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var instructionsPath = Path.Combine(desktopPath, "IIM_WSL_Install_Instructions.txt");
            await File.WriteAllTextAsync(instructionsPath, instructions);

            // Generate IT-friendly PowerShell script
            var scriptPath = Path.Combine(desktopPath, "IIM_WSL_Install_Script.ps1");
            await File.WriteAllTextAsync(scriptPath, GetWslInstallScript());

            _logger.LogInformation("Installation files saved to desktop");
        }

        /// <summary>
        /// Generates PowerShell script for WSL installation.
        /// </summary>
        private string GetWslInstallScript()
        {
            return @"
# IIM Platform WSL2 Installation Script
# Run this script as Administrator
# Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @"

$ErrorActionPreference = 'Stop'

Write-Host 'IIM Platform WSL2 Setup' -ForegroundColor Cyan
Write-Host '======================' -ForegroundColor Cyan
Write-Host ''

# Check if running as admin
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] 'Administrator'))
{
    Write-Host 'This script requires Administrator privileges!' -ForegroundColor Red
    Write-Host 'Please run PowerShell as Administrator and try again.' -ForegroundColor Yellow
    Exit 1
}

Write-Host 'Step 1: Enabling Windows Subsystem for Linux...' -ForegroundColor Green
dism.exe /online /enable-feature /featurename:Microsoft-Windows-Subsystem-Linux /all /norestart
if ($LASTEXITCODE -ne 0) {
    Write-Host 'Failed to enable WSL feature' -ForegroundColor Red
    Exit 1
}

Write-Host 'Step 2: Enabling Virtual Machine Platform...' -ForegroundColor Green
dism.exe /online /enable-feature /featurename:VirtualMachinePlatform /all /norestart
if ($LASTEXITCODE -ne 0) {
    Write-Host 'Failed to enable Virtual Machine Platform' -ForegroundColor Red
    Exit 1
}

Write-Host 'Step 3: Downloading WSL2 kernel update...' -ForegroundColor Green
$kernelUrl = 'https://wslstorestorage.blob.core.windows.net/wslblob/wsl_update_x64.msi'
$kernelPath = '$env:TEMP\wsl_update_x64.msi'
Invoke-WebRequest -Uri $kernelUrl -OutFile $kernelPath -UseBasicParsing

Write-Host 'Step 4: Installing WSL2 kernel update...' -ForegroundColor Green
Start-Process -FilePath 'msiexec.exe' -ArgumentList '/i', $kernelPath, '/quiet' -Wait

Write-Host 'Step 5: Setting WSL2 as default...' -ForegroundColor Green
wsl --set-default-version 2

Write-Host ''
Write-Host 'WSL2 installation complete!' -ForegroundColor Green
Write-Host 'Please restart your computer to complete the setup.' -ForegroundColor Yellow
Write-Host ''
Write-Host 'After restart, run the IIM platform and it will:' -ForegroundColor Cyan
Write-Host '  1. Install Ubuntu distro for services' -ForegroundColor White
Write-Host '  2. Configure Docker and required services' -ForegroundColor White
Write-Host '  3. Optionally install Kali for forensic tools' -ForegroundColor White
Write-Host ''
Write-Host 'Press any key to exit...'
$null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown')
";
        }

        /// <summary>
        /// Enables WSL feature using DISM.
        /// </summary>
        private async Task<bool> EnableWslFeatureAsync()
        {
            try
            {
                var result = await RunCommandAsync("dism.exe",
                    "/online /enable-feature /featurename:Microsoft-Windows-Subsystem-Linux /all /norestart",
                    60000);
                return result.ExitCode == 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enable WSL feature");
                return false;
            }
        }

        /// <summary>
        /// Enables Virtual Machine Platform feature.
        /// </summary>
        private async Task<bool> EnableVirtualMachinePlatformAsync()
        {
            try
            {
                var result = await RunCommandAsync("dism.exe",
                    "/online /enable-feature /featurename:VirtualMachinePlatform /all /norestart",
                    60000);
                return result.ExitCode == 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enable Virtual Machine Platform");
                return false;
            }
        }

        /// <summary>
        /// Downloads and installs WSL2 kernel update.
        /// </summary>
        private async Task<bool> InstallWsl2KernelAsync()
        {
            try
            {
                var kernelPath = Path.Combine(Path.GetTempPath(), "wsl_update_x64.msi");

                // Download kernel update
                using (var response = await _httpClient.GetAsync(WSL_KERNEL_URL))
                {
                    response.EnsureSuccessStatusCode();
                    await using var fs = File.Create(kernelPath);
                    await response.Content.CopyToAsync(fs);
                }

                // Install kernel update
                var result = await RunCommandAsync("msiexec.exe",
                    $"/i \"{kernelPath}\" /quiet",
                    60000);

                return result.ExitCode == 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to install WSL2 kernel");
                return false;
            }
        }

        /// <summary>
        /// Sets WSL2 as the default version.
        /// </summary>
        private async Task<bool> SetWsl2AsDefaultAsync()
        {
            try
            {
                var result = await RunCommandAsync("wsl", "--set-default-version 2", 5000);
                return result.ExitCode == 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set WSL2 as default");
                return false;
            }
        }

        /// <summary>
        /// Installs Ubuntu distro for services.
        /// </summary>
        private async Task<bool> InstallUbuntuDistroAsync(CancellationToken ct)
        {
            try
            {
                // Download Ubuntu 22.04 from Microsoft Store or use local image
                var imagePath = Path.Combine(_configPath, "ubuntu-22.04.tar.gz");

                if (!File.Exists(imagePath))
                {
                    _logger.LogInformation("Downloading Ubuntu 22.04...");
                    // In production, download from secure CDN
                    // For now, use wsl --install command
                    var result = await RunCommandAsync("wsl",
                        $"--install -d Ubuntu-22.04",
                        300000, ct); // 5 minute timeout

                    if (result.ExitCode != 0)
                    {
                        return false;
                    }
                }
                else
                {
                    // Import from local image
                    return await InstallDistroAsync(imagePath, UBUNTU_DISTRO, ct);
                }

                // Rename to IIM-Ubuntu
                await RunCommandAsync("wsl",
                    $"--export Ubuntu-22.04 \"{imagePath}\"",
                    60000, ct);

                await RunCommandAsync("wsl",
                    $"--unregister Ubuntu-22.04",
                    5000, ct);

                return await InstallDistroAsync(imagePath, UBUNTU_DISTRO, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to install Ubuntu distro");
                return false;
            }
        }

        /// <summary>
        /// Installs Kali distro for forensic tools.
        /// </summary>
        private async Task<bool> InstallKaliDistroAsync(CancellationToken ct)
        {
            try
            {
                // Download pre-configured Kali image from IIM CDN
                var imagePath = Path.Combine(_configPath, "kali-iim.tar.gz");

                if (!File.Exists(imagePath))
                {
                    _logger.LogInformation("Downloading pre-configured Kali for IIM...");
                    // Download from secure CDN with MCP server pre-installed
                    // This would be from your model catalogue API

                    // For now, return false as it's optional
                    _logger.LogWarning("Kali image not found. Forensic tools will be unavailable.");
                    return false;
                }

                return await InstallDistroAsync(imagePath, KALI_DISTRO, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to install Kali distro");
                return false;
            }
        }

        /// <summary>
        /// Starts a WSL distro.
        /// </summary>
        private async Task StartDistroAsync(string distroName, CancellationToken ct)
        {
            await RunCommandAsync("wsl", $"-d {distroName} echo 'Starting distro...'", 5000, ct);
        }

        /// <summary>
        /// Checks if a distro is configured.
        /// </summary>
        private async Task<bool> IsDistroConfiguredAsync(string distroName, CancellationToken ct)
        {
            try
            {
                var result = await RunWslCommandAsync(distroName,
                    "test -f /etc/iim-configured && echo 'yes' || echo 'no'", ct);
                return result.Trim() == "yes";
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Configures a distro with required packages and settings.
        /// </summary>
        private async Task ConfigureDistroAsync(string distroName, CancellationToken ct)
        {
            var configScript = distroName == UBUNTU_DISTRO
                ? GetUbuntuConfigScript()
                : GetKaliConfigScript();

            await ExecuteInDistroAsync(distroName, configScript, ct);
            await ExecuteInDistroAsync(distroName, "touch /etc/iim-configured", ct);
        }

        /// <summary>
        /// Gets Ubuntu configuration script.
        /// </summary>
        private string GetUbuntuConfigScript()
        {
            return @"
#!/bin/bash
set -e

# Update system
apt-get update
apt-get upgrade -y

# Install Docker
apt-get install -y docker.io docker-compose
systemctl enable docker
systemctl start docker

# Install required tools
apt-get install -y curl wget git python3 python3-pip rsync

# Create IIM directories
mkdir -p /opt/iim/{models,data,logs}

# Install Python dependencies for embedding service
pip3 install fastapi uvicorn sentence-transformers

echo 'Ubuntu configuration complete'
";
        }

        /// <summary>
        /// Gets Kali configuration script.
        /// </summary>
        private string GetKaliConfigScript()
        {
            return @"
#!/bin/bash
set -e

# Update system
apt-get update
apt-get upgrade -y

# Install MCP server dependencies
apt-get install -y nodejs npm python3 python3-pip

# Install forensic tools
apt-get install -y volatility3 autopsy sleuthkit binwalk foremost

# Setup MCP server
cd /opt
git clone https://github.com/iim-platform/mcp-forensics.git
cd mcp-forensics
npm install
npm run build

# Create systemd service for MCP
cat > /etc/systemd/system/mcp-forensics.service << EOF
[Unit]
Description=MCP Forensics Server
After=network.target

[Service]
Type=simple
User=root
WorkingDirectory=/opt/mcp-forensics
ExecStart=/usr/bin/node /opt/mcp-forensics/dist/server.js
Restart=always

[Install]
WantedBy=multi-user.target
EOF

systemctl enable mcp-forensics
systemctl start mcp-forensics

echo 'Kali configuration complete'
";
        }

        /// <summary>
        /// Generates startup script for services.
        /// </summary>
        private string GenerateStartupScript()
        {
            return @"
#!/bin/bash
set -e

echo 'Starting IIM services...'

# Start Docker if not running
if ! systemctl is-active --quiet docker; then
    systemctl start docker
fi

# Start Qdrant
docker run -d --name qdrant \
    -p 6333:6333 \
    -v $HOME/qdrant:/qdrant/storage \
    --restart always \
    qdrant/qdrant:latest

# Start PostgreSQL with auto-generated credentials
POSTGRES_PASSWORD=$(openssl rand -base64 32)
echo ""POSTGRES_PASSWORD=$POSTGRES_PASSWORD"" > /opt/iim/.postgres_creds

docker run -d --name postgres \
    -p 5432:5432 \
    -e POSTGRES_PASSWORD=$POSTGRES_PASSWORD \
    -e POSTGRES_DB=iim \
    -v $HOME/postgres:/var/lib/postgresql/data \
    --restart always \
    postgres:15

# Start MinIO for evidence storage
MINIO_ROOT_USER=iim_admin
MINIO_ROOT_PASSWORD=$(openssl rand -base64 32)
echo ""MINIO_ROOT_USER=$MINIO_ROOT_USER"" > /opt/iim/.minio_creds
echo ""MINIO_ROOT_PASSWORD=$MINIO_ROOT_PASSWORD"" >> /opt/iim/.minio_creds

docker run -d --name minio \
    -p 9000:9000 \
    -p 9001:9001 \
    -e MINIO_ROOT_USER=$MINIO_ROOT_USER \
    -e MINIO_ROOT_PASSWORD=$MINIO_ROOT_PASSWORD \
    -v $HOME/minio:/data \
    --restart always \
    minio/minio server /data --console-address ':9001'

echo 'All services started successfully'
";
        }

        /// <summary>
        /// Checks health of services in WSL.
        /// </summary>
        private async Task<ServiceHealthCheck> CheckServicesHealthAsync(WslDistro distro, CancellationToken ct)
        {
            var health = new ServiceHealthCheck { IsHealthy = true };
            var details = new List<string>();

            var network = await GetNetworkInfoAsync(distro.Name, ct);

            foreach (var service in _servicePorts)
            {
                try
                {
                    if (!string.IsNullOrEmpty(network.WslIpAddress))
                    {
                        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                        cts.CancelAfter(TimeSpan.FromSeconds(5));

                        var healthUrl = service.Key == "minio"
                            ? $"http://{network.WslIpAddress}:{service.Value}/minio/health/live"
                            : $"http://{network.WslIpAddress}:{service.Value}/";

                        var response = await _httpClient.GetAsync(healthUrl, cts.Token);

                        if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        {
                            details.Add($"✓ {service.Key} is running on port {service.Value}");
                        }
                        else
                        {
                            health.IsHealthy = false;
                            details.Add($"✗ {service.Key} returned {response.StatusCode}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Service not responding is expected initially
                    details.Add($"⚠ {service.Key} is starting up...");
                    _logger.LogDebug("Service {Service} not yet ready: {Message}",
                        service.Key, ex.Message);
                }
            }

            health.Details = string.Join("\n", details);
            return health;
        }

        /// <summary>
        /// Diagnoses and attempts to fix service issues.
        /// </summary>
        private async Task DiagnoseAndFixServicesAsync(WslDistro distro, ServiceHealthCheck health, CancellationToken ct)
        {
            _logger.LogInformation("Diagnosing service issues in {Distro}", distro.Name);

            // Check Docker status
            var dockerStatus = await ExecuteInDistroAsync(distro.Name,
                "systemctl is-active docker || service docker status", ct);

            if (dockerStatus.ExitCode != 0)
            {
                _logger.LogWarning("Docker not running, attempting to start...");
                await ExecuteInDistroAsync(distro.Name, "systemctl start docker || service docker start", ct);
                await Task.Delay(3000, ct);
            }

            // Check and restart individual containers if needed
            foreach (var service in _servicePorts.Keys)
            {
                var containerStatus = await ExecuteInDistroAsync(distro.Name,
                    $"docker ps --filter name={service} --format '{{{{.Status}}}}'", ct);

                if (string.IsNullOrWhiteSpace(containerStatus.StandardOutput))
                {
                    _logger.LogWarning("Container {Service} not found, starting...", service);

                    // Remove old container if exists
                    await ExecuteInDistroAsync(distro.Name, $"docker rm -f {service} 2>/dev/null || true", ct);

                    // Start container based on service type
                    var startCommand = GetServiceStartCommand(service);
                    await ExecuteInDistroAsync(distro.Name, startCommand, ct);
                }
            }
        }

        /// <summary>
        /// Gets the start command for a specific service.
        /// </summary>
        private string GetServiceStartCommand(string service)
        {
            return service switch
            {
                "qdrant" => "docker run -d --name qdrant -p 6333:6333 -v $HOME/qdrant:/qdrant/storage --restart always qdrant/qdrant:latest",
                "postgres" => "docker run -d --name postgres -p 5432:5432 -e POSTGRES_PASSWORD=iim_secure_pass -e POSTGRES_DB=iim -v $HOME/postgres:/var/lib/postgresql/data --restart always postgres:15",
                "minio" => "docker run -d --name minio -p 9000:9000 -p 9001:9001 -e MINIO_ROOT_USER=iim_admin -e MINIO_ROOT_PASSWORD=iim_secure_pass -v $HOME/minio:/data --restart always minio/minio server /data --console-address ':9001'",
                _ => throw new NotSupportedException($"Service {service} not supported")
            };
        }

        /// <summary>
        /// Gets WSL memory information.
        /// </summary>
        private async Task<(double TotalGb, double AvailableGb)> GetWslMemoryInfoAsync(CancellationToken ct)
        {
            try
            {
                var result = await RunWslCommandAsync(UBUNTU_DISTRO,
                    "free -g | grep Mem | awk '{print $2,$7}'", ct);

                var parts = result.Trim().Split(' ');
                if (parts.Length >= 2 &&
                    double.TryParse(parts[0], out var total) &&
                    double.TryParse(parts[1], out var available))
                {
                    return (total, available);
                }
            }
            catch
            {
                // Ignore
            }

            return (0, 0);
        }

        /// <summary>
        /// Runs a command in Windows and returns the result.
        /// </summary>
        private async Task<CommandResult> RunCommandAsync(string command, string arguments, int timeoutMs, CancellationToken ct = default)
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeoutMs);

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync(cts.Token);

            return new CommandResult
            {
                ExitCode = process.ExitCode,
                StandardOutput = await outputTask,
                StandardError = await errorTask
            };
        }

        /// <summary>
        /// Runs a command inside a WSL distro.
        /// </summary>
        private async Task<string> RunWslCommandAsync(string distro, string command, CancellationToken ct)
        {
            var result = await ExecuteInDistroAsync(distro, command, ct);
            return result.StandardOutput;
        }

        /// <summary>
        /// Executes a command in a specific WSL distro.
        /// </summary>
        private async Task<CommandResult> ExecuteInDistroAsync(string distro, string command, CancellationToken ct)
        {
            return await RunCommandAsync("wsl",
                $"-d {distro} -u root -- bash -c \"{command.Replace("\"", "\\\"")}\"",
                COMMAND_TIMEOUT_MS, ct);
        }

        #endregion
    }

    #region Supporting Types

    /// <summary>
    /// WSL status information
    /// </summary>
    public sealed class WslStatus
    {
        public bool IsInstalled { get; set; }
        public bool IsWsl2 { get; set; }
        public string? Version { get; set; }
        public string? KernelVersion { get; set; }
        public bool VirtualMachinePlatform { get; set; }
        public bool HyperV { get; set; }
        public List<WslDistro> InstalledDistros { get; set; } = new();
        public bool HasIimDistro { get; set; }
        public bool IsReady { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
    }

    /// <summary>
    /// WSL distribution information
    /// </summary>
    public sealed class WslDistro
    {
        public required string Name { get; init; }
        public WslDistroState State { get; init; }
        public string? Version { get; init; }
        public string? IpAddress { get; set; }
        public bool IsDefault { get; init; }
    }

    /// <summary>
    /// WSL network information
    /// </summary>
    public sealed class WslNetworkInfo
    {
        public required string DistroName { get; init; }
        public string? WslIpAddress { get; set; }
        public string? WindowsHostIp { get; set; }
        public string? WindowsWslInterface { get; set; }
        public Dictionary<string, string> ServiceEndpoints { get; set; } = new();
        public bool IsConnected { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Service health check result
    /// </summary>
    public sealed class ServiceHealthCheck
    {
        public bool IsHealthy { get; set; }
        public string Details { get; set; } = string.Empty;
    }

    /// <summary>
    /// Overall health check result
    /// </summary>
    public sealed class HealthCheckResult
    {
        public bool IsHealthy { get; set; }
        public bool WslReady { get; set; }
        public bool DistroRunning { get; set; }
        public bool ServicesHealthy { get; set; }
        public bool NetworkConnected { get; set; }
        public IEnumerable<string> Issues { get; set; } = Array.Empty<string>();
        public DateTimeOffset Timestamp { get; set; }
    }

    /// <summary>
    /// Command execution result
    /// </summary>
    public sealed class CommandResult
    {
        public int ExitCode { get; init; }
        public string StandardOutput { get; init; } = string.Empty;
        public string StandardError { get; init; } = string.Empty;
    }

    /// <summary>
    /// WSL distro state
    /// </summary>
    public enum WslDistroState
    {
        Unknown,
        Running,
        Stopped,
        Installing,
        Error
    }

    #endregion
}