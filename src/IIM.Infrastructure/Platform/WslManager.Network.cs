// ============================================
// File: src/IIM.Infrastructure/Platform/WslManager.Network.cs
// Purpose: Network, proxy, and health monitoring operations
// Author: IIM Platform Team
// Created: 2024
// ============================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using IIM.Infrastructure.Platform.Models;
using Microsoft.Extensions.Logging;

namespace IIM.Infrastructure.Platform
{
    /// <summary>
    /// Network and health monitoring operations
    /// </summary>
    public sealed partial class WslManager
    {
        /// <summary>
        /// Gets network information for a WSL distribution.
        /// </summary>
        public async Task<WslNetworkInfo> GetNetworkInfoAsync(string distroName, CancellationToken ct = default)
        {
            var networkInfo = new WslNetworkInfo
            {
                DistroName = distroName
            };

            try
            {
                // Get WSL IP address
                var ipResult = await ExecuteCommandAsync(distroName,
                    "hostname -I | awk '{print $1}'", ct);
                
                if (ipResult.ExitCode == 0)
                {
                    networkInfo.WslIpAddress = ipResult.StandardOutput.Trim();
                }

                // Get Windows host IP as seen from WSL
                var hostResult = await ExecuteCommandAsync(distroName,
                    "ip route | grep default | awk '{print $3}'", ct);
                
                if (hostResult.ExitCode == 0)
                {
                    networkInfo.WindowsHostIp = hostResult.StandardOutput.Trim();
                }

                // Get Windows WSL interface
                var interfaceResult = await RunCommandAsync("powershell",
                    "Get-NetIPAddress -AddressFamily IPv4 | Where-Object {$_.IPAddress -like '172.*'} | Select-Object -First 1 -ExpandProperty IPAddress",
                    5000, ct);
                
                if (interfaceResult.ExitCode == 0)
                {
                    networkInfo.WindowsWslInterface = interfaceResult.StandardOutput.Trim();
                }

                // Build service endpoints
                if (!string.IsNullOrEmpty(networkInfo.WindowsHostIp))
                {
                    foreach (var (service, port) in _servicePorts)
                    {
                        networkInfo.ServiceEndpoints[service] = $"http://{networkInfo.WindowsHostIp}:{port}";
                    }
                }

                // Test connectivity
                networkInfo.IsConnected = await TestConnectivityAsync(networkInfo, ct);

                // Measure latency
                if (networkInfo.IsConnected && !string.IsNullOrEmpty(networkInfo.WslIpAddress))
                {
                    networkInfo.LatencyMs = await MeasureLatencyAsync(networkInfo.WslIpAddress, ct);
                }

                _logger.LogInformation("Network info for {Distro}: WSL IP={WslIp}, Host IP={HostIp}, Connected={Connected}",
                    distroName, networkInfo.WslIpAddress, networkInfo.WindowsHostIp, networkInfo.IsConnected);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get network info for {Distro}", distroName);
                networkInfo.ErrorMessage = ex.Message;
            }

            return networkInfo;
        }

        /// <summary>
        /// Installs Tor and configures proxy settings for WSL.
        /// </summary>
        public async Task InstallTorAndApplyProxyAsync(string windowsProxyPath, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Installing Tor and configuring proxy from {Path}", windowsProxyPath);

                // Convert Windows path to WSL format
                string wslProxyPath = ConvertToWslPath(windowsProxyPath);

                // Bash script to install Tor and apply proxy
                string bashScript = $@"
                    set -e
                    
                    # Apply proxy environment variables if file exists
                    if [ -f ""{wslProxyPath}"" ]; then
                        export $(cat ""{wslProxyPath}"" | xargs)
                    fi
                    
                    # Install Tor if not present
                    if ! command -v tor &> /dev/null; then
                        echo 'Installing Tor...'
                        apt-get update
                        apt-get install -y tor
                    fi
                    
                    # Configure Tor
                    cat > /etc/tor/torrc << EOF
SocksPort 9050
ControlPort 9051
CookieAuthentication 1
EOF
                    
                    # Start Tor service
                    service tor start
                    
                    # Wait for Tor to be ready
                    sleep 5
                    
                    # Test Tor connection
                    curl --socks5 127.0.0.1:9050 --socks5-hostname 127.0.0.1:9050 https://check.torproject.org/api/ip
                ";

                var result = await ExecuteCommandAsync(UBUNTU_DISTRO, bashScript, ct);
                
                if (result.ExitCode == 0)
                {
                    _logger.LogInformation("Tor installed and proxy configured successfully");
                }
                else
                {
                    _logger.LogError("Failed to install Tor: {Error}", result.StandardError);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to install Tor and configure proxy");
                throw;
            }
        }

        /// <summary>
        /// Configures proxy settings for WSL distributions.
        /// </summary>
        public async Task<bool> ConfigureProxyAsync(ProxyConfig config, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Configuring proxy settings");

                var proxyScript = @"
                    # Configure system-wide proxy
                    cat > /etc/environment << EOF
";
                if (!string.IsNullOrEmpty(config.HttpProxy))
                {
                    proxyScript += $"http_proxy={config.HttpProxy}\nHTTP_PROXY={config.HttpProxy}\n";
                }
                
                if (!string.IsNullOrEmpty(config.HttpsProxy))
                {
                    proxyScript += $"https_proxy={config.HttpsProxy}\nHTTPS_PROXY={config.HttpsProxy}\n";
                }
                
                if (!string.IsNullOrEmpty(config.NoProxy))
                {
                    proxyScript += $"no_proxy={config.NoProxy}\nNO_PROXY={config.NoProxy}\n";
                }
                
                proxyScript += "EOF\n";

                // Configure apt proxy
                if (!string.IsNullOrEmpty(config.HttpProxy))
                {
                    proxyScript += $@"
                        cat > /etc/apt/apt.conf.d/proxy.conf << EOF
Acquire::http::Proxy ""{config.HttpProxy}"";
Acquire::https::Proxy ""{config.HttpsProxy ?? config.HttpProxy}"";
EOF
                    ";
                }

                // Configure Docker proxy if needed
                proxyScript += @"
if [ -d /etc/systemd/system/docker.service.d ]; then
    sudo bash -c 'cat > /etc/systemd/system/docker.service.d/proxy.conf' << 'EOF'
[Service]
";
                if (!string.IsNullOrEmpty(config.HttpProxy))
                    proxyScript += $"Environment=\"HTTP_PROXY={config.HttpProxy}\"\n";
                if (!string.IsNullOrEmpty(config.HttpsProxy))
                    proxyScript += $"Environment=\"HTTPS_PROXY={config.HttpsProxy}\"\n";
                if (!string.IsNullOrEmpty(config.NoProxy))
                    proxyScript += $"Environment=\"NO_PROXY={config.NoProxy}\"\n";
                proxyScript += @"EOF
    sudo systemctl daemon-reload
    sudo systemctl restart docker
fi
";


                proxyScript += @"
EOF
                        systemctl daemon-reload
                        systemctl restart docker
                    fi
                ";

                var result = await ExecuteCommandAsync(UBUNTU_DISTRO, proxyScript, ct);
                return result.ExitCode == 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to configure proxy");
                return false;
            }
        }

        /// <summary>
        /// Performs a comprehensive health check of WSL and all services.
        /// </summary>
        public async Task<HealthCheckResult> HealthCheckAsync(CancellationToken ct = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var issues = new List<string>();
            var serviceChecks = new List<ServiceHealthCheck>();

            var result = new HealthCheckResult
            {
                Timestamp = DateTimeOffset.UtcNow
            };

            try
            {
                // Check WSL status
                var wslStatus = await GetStatusAsync(ct);
                result.WslReady = wslStatus.IsReady;
                
                if (!wslStatus.IsInstalled)
                {
                    issues.Add("WSL is not installed");
                }
                else if (!wslStatus.IsWsl2)
                {
                    issues.Add("WSL2 is required but not enabled");
                }

                // Check distro status
                if (wslStatus.HasIimDistro)
                {
                    var runningDistro = wslStatus.InstalledDistros
                        .FirstOrDefault(d => d.Name == UBUNTU_DISTRO && d.State == WslDistroState.Running);
                    
                    result.DistroRunning = runningDistro != null;
                    
                    if (!result.DistroRunning)
                    {
                        issues.Add($"{UBUNTU_DISTRO} is not running");
                    }
                }
                else
                {
                    result.DistroRunning = false;
                    issues.Add("IIM distribution not found");
                }

                // Check network connectivity
                if (result.DistroRunning)
                {
                    var networkInfo = await GetNetworkInfoAsync(UBUNTU_DISTRO, ct);
                    result.NetworkConnected = networkInfo.IsConnected;
                    
                    if (!networkInfo.IsConnected)
                    {
                        issues.Add("Network connectivity issues");
                    }
                }

                // Check services
                if (result.DistroRunning)
                {
                    foreach (var (serviceName, port) in _servicePorts)
                    {
                        var serviceCheck = await CheckServiceHealthAsync(serviceName, ct);
                        serviceChecks.Add(serviceCheck);
                        
                        if (!serviceCheck.IsHealthy)
                        {
                            issues.Add($"Service {serviceName} is unhealthy");
                        }
                    }
                }

                result.ServicesHealthy = serviceChecks.All(s => s.IsHealthy);
                result.ServiceChecks = serviceChecks;
                result.Issues = issues;
                result.IsHealthy = result.WslReady && 
                                  result.DistroRunning && 
                                  result.ServicesHealthy && 
                                  result.NetworkConnected;

                stopwatch.Stop();
                result.ElapsedMs = stopwatch.ElapsedMilliseconds;

                _logger.LogInformation("Health check completed in {Elapsed}ms. Healthy: {IsHealthy}",
                    result.ElapsedMs, result.IsHealthy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                issues.Add($"Health check error: {ex.Message}");
                result.Issues = issues;
                result.IsHealthy = false;
            }

            return result;
        }

        /// <summary>
        /// Checks the health of a specific service.
        /// </summary>
        public async Task<ServiceHealthCheck> CheckServiceHealthAsync(string serviceName, CancellationToken ct = default)
        {
            var health = new ServiceHealthCheck
            {
                ServiceName = serviceName
            };

            try
            {
                var stopwatch = Stopwatch.StartNew();

                // Check if container is running
                var statusResult = await ExecuteCommandAsync(UBUNTU_DISTRO,
                    $"docker ps --filter name=iim-{serviceName} --format '{{{{.Status}}}}'", ct);

                if (statusResult.ExitCode != 0 || string.IsNullOrWhiteSpace(statusResult.StandardOutput))
                {
                    health.IsHealthy = false;
                    health.Details = "Container not running";
                    return health;
                }

                // Get port from configuration
                if (_servicePorts.TryGetValue(serviceName, out var port))
                {
                    health.Port = port;
                    
                    // Test service endpoint
                    var healthEndpoint = serviceName switch
                    {
                        "qdrant" => $"http://localhost:{port}/health",
                        "postgres" => null, // Use docker health check
                        "minio" => $"http://localhost:{port}/minio/health/live",
                        "mcp-server" => $"http://localhost:{port}/health",
                        _ => null
                    };

                    if (healthEndpoint != null)
                    {
                        try
                        {
                            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                            using var response = await _httpClient.GetAsync(healthEndpoint, cts.Token);
                            
                            health.IsHealthy = response.IsSuccessStatusCode;
                            health.Details = $"HTTP {(int)response.StatusCode}";
                        }
                        catch (Exception ex)
                        {
                            health.IsHealthy = false;
                            health.Details = $"Connection failed: {ex.Message}";
                        }
                    }
                    else
                    {
                        // Use Docker health check
                        var healthResult = await ExecuteCommandAsync(UBUNTU_DISTRO,
                            $"docker inspect --format='{{{{.State.Health.Status}}}}' iim-{serviceName}", ct);
                        
                        health.IsHealthy = healthResult.StandardOutput.Trim() == "healthy";
                        health.Details = healthResult.StandardOutput.Trim();
                    }
                }

                stopwatch.Stop();
                health.ResponseTimeMs = stopwatch.ElapsedMilliseconds;

                // Get memory usage
                var statsResult = await ExecuteCommandAsync(UBUNTU_DISTRO,
                    $"docker stats --no-stream --format '{{{{.MemUsage}}}}' iim-{serviceName}", ct);
                
                if (statsResult.ExitCode == 0)
                {
                    var memUsage = statsResult.StandardOutput.Trim();
                    // Parse memory usage (e.g., "1.5GiB / 4GiB")
                    var match = Regex.Match(memUsage, @"([\d.]+)([GMK]i?B)");
                    if (match.Success)
                    {
                        var value = double.Parse(match.Groups[1].Value);
                        var unit = match.Groups[2].Value;
                        health.MemoryUsageBytes = unit switch
                        {
                            "GiB" or "GB" => (long)(value * 1024 * 1024 * 1024),
                            "MiB" or "MB" => (long)(value * 1024 * 1024),
                            "KiB" or "KB" => (long)(value * 1024),
                            _ => (long)value
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check health for {Service}", serviceName);
                health.IsHealthy = false;
                health.Details = $"Health check failed: {ex.Message}";
            }

            return health;
        }

        // ========================================
        // Private Network Helper Methods
        // ========================================

        /// <summary>
        /// Tests network connectivity.
        /// </summary>
        private async Task<bool> TestConnectivityAsync(WslNetworkInfo networkInfo, CancellationToken ct)
        {
            try
            {
                // Test WSL to Windows connectivity
                if (!string.IsNullOrEmpty(networkInfo.WindowsHostIp))
                {
                    var pingResult = await ExecuteCommandAsync(networkInfo.DistroName,
                        $"ping -c 1 -W 2 {networkInfo.WindowsHostIp}", ct);
                    
                    return pingResult.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Connectivity test failed");
            }

            return false;
        }

        /// <summary>
        /// Measures network latency.
        /// </summary>
        private async Task<double> MeasureLatencyAsync(string ipAddress, CancellationToken ct)
        {
            try
            {
                var pingResult = await RunCommandAsync("ping",
                    $"-n 4 {ipAddress}", 5000, ct);
                
                if (pingResult.ExitCode == 0)
                {
                    // Parse average latency from ping output
                    var match = Regex.Match(pingResult.StandardOutput, @"Average = (\d+)ms");
                    if (match.Success)
                    {
                        return double.Parse(match.Groups[1].Value);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to measure latency");
            }

            return -1;
        }

        /// <summary>
        /// Converts Windows path to WSL path format.
        /// </summary>
        private string ConvertToWslPath(string windowsPath)
        {
            // Convert C:\Path\To\File to /mnt/c/Path/To/File
            return $"/mnt/{windowsPath.ToLower()[0]}{windowsPath.Substring(2).Replace('\\', '/')}";
        }
    }
}