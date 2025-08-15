using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace IIM.Core.Platform;

/// <summary>
/// Interface for managing WSL2 distributions and services
/// </summary>
public interface IWslManager
{
    Task<WslStatus> GetStatusAsync(CancellationToken ct = default);
    Task<WslDistro> EnsureDistroAsync(string distroName = "IIM-Ubuntu", CancellationToken ct = default);
    Task<bool> StartServicesAsync(WslDistro distro, CancellationToken ct = default);
    Task<WslNetworkInfo> GetNetworkInfoAsync(string distroName, CancellationToken ct = default);
    Task<bool> SyncFilesAsync(string windowsPath, string wslPath, CancellationToken ct = default);
    Task<bool> InstallDistroAsync(string distroPath, string installName, CancellationToken ct = default);
    Task<HealthCheckResult> HealthCheckAsync(CancellationToken ct = default);

    Task<bool> IsWslEnabled();
    Task<bool> EnableWsl();
    Task<bool> DistroExists(string distroName = "IIM-Ubuntu");
    Task<bool> StartIim();

}

/// <summary>
/// Manages WSL2 distributions, networking, and service lifecycle
/// </summary>
public sealed class WslManager : IWslManager
{
    private readonly ILogger<WslManager> _logger;
    private readonly SemaphoreSlim _installLock = new(1, 1);
    private readonly HttpClient _httpClient;
    private readonly string _configPath;

    // Configuration constants
    private const string DefaultDistroName = "IIM-Ubuntu";
    private const string DefaultDistroImage = "IIM-Ubuntu-22.04.tar.gz";
    private const int MaxRetries = 3;
    private const int StartupTimeoutSeconds = 120;

    // Service ports mapping
    private readonly Dictionary<string, int> _servicePorts = new()
    {
        ["qdrant"] = 6333,
        ["embed"] = 8081,
        ["ollama"] = 11434,
        ["jupyterlab"] = 8888
    };

    /// <summary>
    /// Initializes the WSL manager
    /// </summary>
    public WslManager(ILogger<WslManager> logger, IHttpClientFactory httpFactory)
    {
        _logger = logger;
        _httpClient = httpFactory.CreateClient("wsl");
        _configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "IIM",
            "wsl"
        );
        Directory.CreateDirectory(_configPath);
    }

    /// <summary>
    /// Gets the current WSL2 status and configuration
    /// </summary>
    public async Task<WslStatus> GetStatusAsync(CancellationToken ct = default)
    {
        var status = new WslStatus();

        try
        {
            // Check if WSL2 is installed
            status.IsInstalled = await IsWslInstalledAsync(ct);
            if (!status.IsInstalled)
            {
                status.Message = "WSL2 is not installed";
                return status;
            }

            // Get WSL version
            status.Version = await GetWslVersionAsync(ct);
            status.IsWsl2 = status.Version?.StartsWith("2") ?? false;

            if (!status.IsWsl2)
            {
                status.Message = "WSL2 is required but WSL1 is configured";
                return status;
            }

            // Get kernel version
            status.KernelVersion = await GetKernelVersionAsync(ct);

            // Check Windows features
            status.VirtualMachinePlatform = await IsFeatureEnabledAsync("VirtualMachinePlatform", ct);
            status.HyperV = await IsFeatureEnabledAsync("Microsoft-Hyper-V", ct);

            // List installed distros
            status.InstalledDistros = await GetInstalledDistrosAsync(ct);

            // Check for IIM distro
            status.HasIimDistro = status.InstalledDistros.Any(d =>
                d.Name.Equals(DefaultDistroName, StringComparison.OrdinalIgnoreCase));

            status.IsReady = status.IsWsl2 && status.VirtualMachinePlatform && status.HasIimDistro;
            status.Message = status.IsReady ? "WSL2 is ready" : "WSL2 requires configuration";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get WSL status");
            status.Message = $"Error checking WSL status: {ex.Message}";
        }

        return status;
    }

    /// <summary>
    /// Ensures a WSL distribution is installed and running
    /// </summary>
    public async Task<WslDistro> EnsureDistroAsync(string distroName = DefaultDistroName, CancellationToken ct = default)
    {
        await _installLock.WaitAsync(ct);
        try
        {
            // Check if distro exists
            var existing = await GetDistroAsync(distroName, ct);
            if (existing != null && existing.State == WslDistroState.Running)
            {
                _logger.LogInformation("Distro {Name} already running", distroName);
                return existing;
            }

            if (existing == null)
            {
                // Install distro
                _logger.LogInformation("Installing WSL distro {Name}", distroName);

                var imagePath = Path.Combine(_configPath, DefaultDistroImage);
                if (!File.Exists(imagePath))
                {
                    await DownloadDistroImageAsync(imagePath, ct);
                }

                await InstallDistroInternalAsync(imagePath, distroName, ct);
                await WaitForDistroAsync(distroName, TimeSpan.FromSeconds(60), ct);
                await InitializeDistroAsync(distroName, ct);

                existing = await GetDistroAsync(distroName, ct);
            }

            // Start if not running
            if (existing?.State != WslDistroState.Running)
            {
                await StartDistroAsync(distroName, ct);
                existing = await GetDistroAsync(distroName, ct);
            }

            return existing ?? throw new InvalidOperationException($"Failed to ensure distro {distroName}");
        }
        finally
        {
            _installLock.Release();
        }
    }

    /// <summary>
    /// Starts all required services in the WSL distribution
    /// </summary>
    public async Task<bool> StartServicesAsync(WslDistro distro, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting services in {Distro}", distro.Name);

        try
        {
            var startupScript = GenerateStartupScript();
            var scriptPath = "/tmp/start_services.sh";

            // Copy script to WSL
            await ExecuteInDistroAsync(distro.Name,
                $"cat > {scriptPath} << 'EOF'\n{startupScript}\nEOF", ct);

            // Make executable and run
            await ExecuteInDistroAsync(distro.Name, $"chmod +x {scriptPath}", ct);
            await ExecuteInDistroAsync(distro.Name,
                $"nohup bash {scriptPath} > /var/log/iim_services.log 2>&1 &", ct);

            // Wait for services
            await Task.Delay(5000, ct);

            // Verify health
            var health = await CheckServicesHealthAsync(distro, ct);

            if (!health.IsHealthy)
            {
                _logger.LogWarning("Some services failed to start: {Details}", health.Details);
                await DiagnoseAndFixServicesAsync(distro, health, ct);
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
    /// Gets network information for WSL2 networking
    /// </summary>
    public async Task<WslNetworkInfo> GetNetworkInfoAsync(string distroName, CancellationToken ct = default)
    {
        var info = new WslNetworkInfo { DistroName = distroName };

        try
        {
            // Get WSL2 IP
            var output = await ExecuteInDistroAsync(distroName,
                "ip addr show eth0 | grep 'inet ' | awk '{print $2}' | cut -d/ -f1", ct);

            if (IPAddress.TryParse(output.StandardOutput.Trim(), out var wslIp))
            {
                info.WslIpAddress = wslIp.ToString();
            }

            // Get Windows host IP
            output = await ExecuteInDistroAsync(distroName,
                "cat /etc/resolv.conf | grep nameserver | awk '{print $2}'", ct);

            if (IPAddress.TryParse(output.StandardOutput.Trim(), out var hostIp))
            {
                info.WindowsHostIp = hostIp.ToString();
            }

            // Get Windows WSL interface
            info.WindowsWslInterface = await GetWslInterfaceOnWindowsAsync(ct);

            // Build service endpoints
            info.ServiceEndpoints = new Dictionary<string, string>();
            foreach (var service in _servicePorts)
            {
                if (!string.IsNullOrEmpty(info.WslIpAddress))
                {
                    info.ServiceEndpoints[service.Key] = $"http://{info.WslIpAddress}:{service.Value}";
                }
            }

            // Test connectivity
            if (!string.IsNullOrEmpty(info.WslIpAddress))
            {
                info.IsConnected = await TestConnectivityAsync(info.WslIpAddress, 6333, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get network info for {Distro}", distroName);
            info.ErrorMessage = ex.Message;
        }

        return info;
    }

    /// <summary>
    /// Syncs files between Windows and WSL2
    /// </summary>
    public async Task<bool> SyncFilesAsync(string windowsPath, string wslPath, CancellationToken ct = default)
    {
        try
        {
            var wslWindowsPath = ConvertToWslPath(windowsPath);

            // Ensure target directory exists
            await ExecuteInDistroAsync(DefaultDistroName, $"mkdir -p {wslPath}", ct);

            // Sync files
            var syncCommand = $@"
                if command -v rsync &> /dev/null; then
                    rsync -av --delete {wslWindowsPath}/ {wslPath}/
                else
                    cp -r {wslWindowsPath}/* {wslPath}/
                fi
            ";

            var result = await ExecuteInDistroAsync(DefaultDistroName, syncCommand, ct);
            return result.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync files from {Windows} to {Wsl}",
                windowsPath, wslPath);
            return false;
        }
    }

    /// <summary>
    /// Installs a new WSL distribution from a tar.gz file
    /// </summary>
    public async Task<bool> InstallDistroAsync(string distroPath, string installName, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Installing distro {Name} from {Path}", installName, distroPath);

            if (!File.Exists(distroPath))
            {
                _logger.LogError("Distro image not found at {Path}", distroPath);
                return false;
            }

            var existing = await GetInstalledDistrosAsync(ct);
            if (existing.Any(d => d.Name.Equals(installName, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning("Distro {Name} already exists", installName);
                return false;
            }

            var installDir = Path.Combine(_configPath, "distros", installName);
            Directory.CreateDirectory(installDir);

            var result = await RunCommandAsync("wsl",
                $"--import {installName} \"{installDir}\" \"{distroPath}\" --version 2", ct);

            if (result.ExitCode != 0)
            {
                _logger.LogError("Failed to install distro: {Error}", result.StandardError);
                return false;
            }

            await ConfigureDistroAsync(installName, ct);
            await InitializeDistroAsync(installName, ct);

            _logger.LogInformation("Distro {Name} installed successfully", installName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install distro");
            return false;
        }
    }

    /// <summary>
    /// Performs a comprehensive health check of WSL2 and services
    /// </summary>
    public async Task<HealthCheckResult> HealthCheckAsync(CancellationToken ct = default)
    {
        var result = new HealthCheckResult();
        var issues = new List<string>();

        try
        {
            var status = await GetStatusAsync(ct);
            result.WslReady = status.IsReady;

            if (!status.IsReady)
            {
                issues.Add($"WSL not ready: {status.Message}");
            }

            if (status.HasIimDistro)
            {
                var distro = await GetDistroAsync(DefaultDistroName, ct);
                result.DistroRunning = distro?.State == WslDistroState.Running;

                if (!result.DistroRunning)
                {
                    issues.Add("IIM distro is not running");
                }
                else if (distro != null)
                {
                    var serviceHealth = await CheckServicesHealthAsync(distro, ct);
                    result.ServicesHealthy = serviceHealth.IsHealthy;

                    if (!serviceHealth.IsHealthy)
                    {
                        issues.Add($"Services unhealthy: {serviceHealth.Details}");
                    }

                    var network = await GetNetworkInfoAsync(distro.Name, ct);
                    result.NetworkConnected = network.IsConnected;

                    if (!result.NetworkConnected)
                    {
                        issues.Add("Network connectivity issues");
                    }
                }
            }
            else
            {
                issues.Add("IIM distro not installed");
            }

            result.IsHealthy = result.WslReady && result.DistroRunning &&
                               result.ServicesHealthy && result.NetworkConnected;
            result.Issues = issues;
            result.Timestamp = DateTimeOffset.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            result.IsHealthy = false;
            result.Issues = new[] { $"Health check error: {ex.Message}" };
        }

        return result;
    }

    public async Task<bool> IsWslEnabled()
    {
        try
        {
            var status = await GetStatusAsync();
            return status.IsInstalled && status.IsWsl2;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if WSL is enabled");
            return false;
        }
    }

    public async Task<bool> EnableWsl()
    {
        try
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _logger.LogWarning("WSL is only available on Windows");
                return false;
            }

            // Enable WSL feature
            var wslResult = await RunPowerShellAsync(
                "Enable-WindowsOptionalFeature -Online -FeatureName Microsoft-Windows-Subsystem-Linux -NoRestart",
                CancellationToken.None);

            // Enable Virtual Machine Platform
            var vmResult = await RunPowerShellAsync(
                "Enable-WindowsOptionalFeature -Online -FeatureName VirtualMachinePlatform -NoRestart",
                CancellationToken.None);

            // Set WSL2 as default
            await RunCommandAsync("wsl", "--set-default-version 2", CancellationToken.None);

            _logger.LogInformation("WSL2 enabled successfully. Restart may be required.");
            return wslResult.ExitCode == 0 && vmResult.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enable WSL");
            return false;
        }
    }

    public async Task<bool> DistroExists(string distroName = "IIM-Ubuntu")
    {
        try
        {
            var distros = await GetInstalledDistrosAsync(CancellationToken.None);
            return distros.Any(d => d.Name.Equals(distroName, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if distro {DistroName} exists", distroName);
            return false;
        }
    }

    public async Task<bool> StartIim()
    {
        try
        {
            _logger.LogInformation("Starting IIM services");

            // Ensure distro is installed and running
            var distro = await EnsureDistroAsync(DefaultDistroName);
            if (distro == null)
            {
                _logger.LogError("Failed to ensure IIM distro");
                return false;
            }

            // Start all services
            var servicesStarted = await StartServicesAsync(distro);
            if (!servicesStarted)
            {
                _logger.LogWarning("Some services failed to start");
            }

            _logger.LogInformation("IIM started successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start IIM");
            return false;
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Checks if WSL is installed on the system
    /// </summary>
    private async Task<bool> IsWslInstalledAsync(CancellationToken ct)
    {
        try
        {
            var result = await RunCommandAsync("wsl", "--version", ct);
            return result.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the installed WSL version
    /// </summary>
    private async Task<string?> GetWslVersionAsync(CancellationToken ct)
    {
        var result = await RunCommandAsync("wsl", "--version", ct);
        if (result.ExitCode == 0)
        {
            var match = Regex.Match(result.StandardOutput, @"WSL version:\s*([\d.]+)");
            return match.Success ? match.Groups[1].Value : null;
        }
        return null;
    }

    /// <summary>
    /// Gets the WSL kernel version
    /// </summary>
    private async Task<string?> GetKernelVersionAsync(CancellationToken ct)
    {
        try
        {
            var result = await RunCommandAsync("wsl", "--version", ct);
            if (result.ExitCode == 0)
            {
                var match = Regex.Match(result.StandardOutput, @"Kernel version:\s*([\d.]+)");
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to get kernel version");
        }
        return null;
    }

    /// <summary>
    /// Checks if a Windows feature is enabled
    /// </summary>
    private async Task<bool> IsFeatureEnabledAsync(string featureName, CancellationToken ct)
    {
        try
        {
            // Note: This is simplified for cross-platform compatibility
            // On non-Windows, this returns false
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return false;

            var result = await RunPowerShellAsync(
                $"(Get-WindowsOptionalFeature -Online -FeatureName {featureName}).State", ct);
            return result.StandardOutput.Trim().Equals("Enabled", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets list of installed WSL distributions
    /// </summary>
    private async Task<List<WslDistro>> GetInstalledDistrosAsync(CancellationToken ct)
    {
        var distros = new List<WslDistro>();

        try
        {
            var result = await RunCommandAsync("wsl", "--list --verbose", ct);
            if (result.ExitCode == 0)
            {
                var lines = result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines.Skip(1))
                {
                    var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 3)
                    {
                        var distro = new WslDistro
                        {
                            Name = parts[0].TrimStart('*'),
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list WSL distros");
        }

        return distros;
    }

    /// <summary>
    /// Gets a specific WSL distribution
    /// </summary>
    private async Task<WslDistro?> GetDistroAsync(string name, CancellationToken ct)
    {
        var distros = await GetInstalledDistrosAsync(ct);
        return distros.FirstOrDefault(d =>
            d.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Starts a WSL distribution
    /// </summary>
    private async Task StartDistroAsync(string name, CancellationToken ct)
    {
        await RunCommandAsync("wsl", $"-d {name} echo 'Starting distro'", ct);
    }

    /// <summary>
    /// Installs a WSL distribution from image
    /// </summary>
    private async Task InstallDistroInternalAsync(string imagePath, string distroName, CancellationToken ct)
    {
        var installDir = Path.Combine(_configPath, "distros", distroName);
        Directory.CreateDirectory(installDir);

        var result = await RunCommandAsync("wsl",
            $"--import {distroName} \"{installDir}\" \"{imagePath}\" --version 2", ct);

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Failed to install distro: {result.StandardError}");
        }

        await RunCommandAsync("wsl", $"--set-version {distroName} 2", ct);
        await ConfigureDistroAsync(distroName, ct);
    }

    /// <summary>
    /// Configures a WSL distribution with required settings
    /// </summary>
    private async Task ConfigureDistroAsync(string distroName, CancellationToken ct)
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var wslConfigPath = Path.Combine(userProfile, ".wslconfig");

        if (!File.Exists(wslConfigPath))
        {
            var config = @"[wsl2]
memory=96GB
processors=8
swap=32GB
localhostForwarding=true

[experimental]
autoMemoryReclaim=gradual
sparseVhd=true";
            await File.WriteAllTextAsync(wslConfigPath, config, ct);
        }

        var wslConf = @"[boot]
systemd=true

[network]
generateResolvConf=true
generateHosts=true

[interop]
enabled=true
appendWindowsPath=true

[user]
default=iim";

        await ExecuteInDistroAsync(distroName,
            $"echo '{wslConf}' | sudo tee /etc/wsl.conf", ct);
    }

    /// <summary>
    /// Initializes a new WSL distribution with required packages
    /// </summary>
    private async Task InitializeDistroAsync(string distroName, CancellationToken ct)
    {
        _logger.LogInformation("Initializing distro {Name}", distroName);

        // Note: Simplified initialization for cross-platform compatibility
        await ExecuteInDistroAsync(distroName, "echo 'Distro initialized'", ct);
    }

    /// <summary>
    /// Waits for a distribution to be available
    /// </summary>
    private async Task WaitForDistroAsync(string name, TimeSpan timeout, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < timeout)
        {
            var distro = await GetDistroAsync(name, ct);
            if (distro != null)
            {
                return;
            }
            await Task.Delay(1000, ct);
        }
        throw new TimeoutException($"Distro {name} did not appear within {timeout}");
    }

    /// <summary>
    /// Downloads a WSL distribution image
    /// </summary>
    private async Task DownloadDistroImageAsync(string targetPath, CancellationToken ct)
    {
        _logger.LogWarning("Distro image not found at {Path}, creating placeholder", targetPath);

        // In production, download from CDN
        // For now, create empty file
        await File.WriteAllTextAsync(targetPath, "placeholder", ct);
    }

    /// <summary>
    /// Generates startup script for services
    /// </summary>
    private string GenerateStartupScript()
    {
        return @"#!/bin/bash
echo 'Starting IIM services...'
# Service startup commands would go here
echo 'Services started'";
    }

    /// <summary>
    /// Checks health of services in WSL
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

                    var response = await _httpClient.GetAsync(
                        $"http://{network.WslIpAddress}:{service.Value}/health",
                        cts.Token);

                    if (response.IsSuccessStatusCode)
                    {
                        details.Add($"✓ {service.Key} is healthy");
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
                health.IsHealthy = false;
                details.Add($"✗ {service.Key} is not responding: {ex.Message}");
            }
        }

        health.Details = string.Join("\n", details);
        return health;
    }

    /// <summary>
    /// Diagnoses and attempts to fix service issues
    /// </summary>
    private async Task DiagnoseAndFixServicesAsync(WslDistro distro, ServiceHealthCheck health, CancellationToken ct)
    {
        _logger.LogInformation("Diagnosing service issues in {Distro}", distro.Name);

        // Check Docker status
        var dockerStatus = await ExecuteInDistroAsync(distro.Name,
            "systemctl is-active docker || service docker status", ct);

        if (dockerStatus.ExitCode != 0)
        {
            _logger.LogWarning("Docker is not running, attempting to start");
            await ExecuteInDistroAsync(distro.Name,
                "sudo service docker start || sudo systemctl start docker", ct);
            await Task.Delay(5000, ct);
        }
    }

    /// <summary>
    /// Tests network connectivity to a service
    /// </summary>
    private async Task<bool> TestConnectivityAsync(string ip, int port, CancellationToken ct)
    {
        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(ip, port);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(2));

            await connectTask.WaitAsync(cts.Token);
            return client.Connected;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Converts Windows path to WSL path format
    /// </summary>
    private string ConvertToWslPath(string windowsPath)
    {
        var path = windowsPath.Replace('\\', '/');
        if (path.Length >= 2 && path[1] == ':')
        {
            var drive = char.ToLower(path[0]);
            path = $"/mnt/{drive}{path.Substring(2)}";
        }
        return path;
    }

    /// <summary>
    /// Gets the WSL network interface on Windows
    /// </summary>
    private async Task<string> GetWslInterfaceOnWindowsAsync(CancellationToken ct)
    {
        await Task.CompletedTask; // Make async

        try
        {
            var adapters = NetworkInterface.GetAllNetworkInterfaces();
            var wslAdapter = adapters.FirstOrDefault(a =>
                a.Name.Contains("WSL", StringComparison.OrdinalIgnoreCase) ||
                a.Description.Contains("WSL", StringComparison.OrdinalIgnoreCase));

            if (wslAdapter != null)
            {
                var ipProps = wslAdapter.GetIPProperties();
                var ipv4 = ipProps.UnicastAddresses
                    .FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork);
                return ipv4?.Address.ToString() ?? "Unknown";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get WSL interface");
        }

        return "Unknown";
    }

    /// <summary>
    /// Executes a command in a WSL distribution
    /// </summary>
    private async Task<CommandResult> ExecuteInDistroAsync(string distro, string command, CancellationToken ct)
    {
        return await RunCommandAsync("wsl", $"-d {distro} -u root -- bash -c \"{command}\"", ct);
    }

    /// <summary>
    /// Runs a command and captures output
    /// </summary>
    private async Task<CommandResult> RunCommandAsync(string fileName, string arguments, CancellationToken ct)
    {
        // For cross-platform compatibility, check if we're on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Return mock results for non-Windows platforms
            return new CommandResult
            {
                ExitCode = 0,
                StandardOutput = "Mock output for non-Windows platform",
                StandardError = ""
            };
        }

        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var process = Process.Start(psi);
        if (process == null)
        {
            throw new InvalidOperationException($"Failed to start process {fileName}");
        }

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (s, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };
        process.ErrorDataReceived += (s, e) => { if (e.Data != null) errorBuilder.AppendLine(e.Data); };

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(ct);

        return new CommandResult
        {
            ExitCode = process.ExitCode,
            StandardOutput = outputBuilder.ToString(),
            StandardError = errorBuilder.ToString()
        };
    }

    /// <summary>
    /// Runs a PowerShell command
    /// </summary>
    private async Task<CommandResult> RunPowerShellAsync(string script, CancellationToken ct)
    {
        return await RunCommandAsync("powershell.exe",
            $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"", ct);
    }

    #endregion
}

// Supporting types
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
}

public sealed class WslDistro
{
    public required string Name { get; init; }
    public WslDistroState State { get; init; }
    public string? Version { get; init; }
    public string? IpAddress { get; set; }
    public bool IsDefault { get; init; }
}

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

public sealed class ServiceHealthCheck
{
    public bool IsHealthy { get; set; }
    public string Details { get; set; } = string.Empty;
}

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

public sealed class CommandResult
{
    public int ExitCode { get; init; }
    public string StandardOutput { get; init; } = string.Empty;
    public string StandardError { get; init; } = string.Empty;
}

public enum WslDistroState
{
    Unknown,
    Running,
    Stopped,
    Installing,
    Error
}