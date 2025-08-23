// ============================================
// File: src/IIM.Infrastructure/Platform/WslManager.Distro.cs
// Purpose: WSL distribution management operations
// Author: IIM Platform Team
// Created: 2024
// ============================================

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IIM.Infrastructure.Platform.Models;
using Microsoft.Extensions.Logging;

namespace IIM.Infrastructure.Platform
{
    /// <summary>
    /// WSL distribution management operations
    /// </summary>
    public sealed partial class WslManager
    {
        private const string UBUNTU_DOWNLOAD_URL = "https://aka.ms/wslubuntu2204";
        private const string KALI_DOWNLOAD_URL = "https://aka.ms/wsl-kali-linux-new";

        /// <summary>
        /// Ensures a WSL distribution is installed and configured.
        /// </summary>
        public async Task<WslDistro> EnsureDistroAsync(string distroName = "IIM-Ubuntu", CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Ensuring distro {DistroName} is installed", distroName);

                // Check if distro exists
                if (await DistroExists(distroName))
                {
                    _logger.LogInformation("Distro {DistroName} already exists", distroName);

                    // Get distro info
                    var distros = await GetInstalledDistrosAsync(ct);
                    var distro = distros.FirstOrDefault(d => d.Name == distroName);

                    if (distro != null)
                    {
                        // Ensure it's running
                        if (distro.State != WslDistroState.Running)
                        {
                            await StartDistroAsync(distroName, ct);
                            distro.State = WslDistroState.Running;
                        }

                        // Get IP address
                        var networkInfo = await GetNetworkInfoAsync(distroName, ct);
                        distro.IpAddress = networkInfo.WslIpAddress;

                        return distro;
                    }
                }

                // Download and install distro
                var downloadUrl = distroName switch
                {
                    UBUNTU_DISTRO => UBUNTU_DOWNLOAD_URL,
                    KALI_DISTRO => KALI_DOWNLOAD_URL,
                    _ => throw new NotSupportedException($"Distro {distroName} is not supported")
                };

                var tarPath = await DownloadDistroAsync(downloadUrl, distroName, ct);
                if (string.IsNullOrEmpty(tarPath))
                {
                    throw new InvalidOperationException($"Failed to download {distroName}");
                }

                // Install the distro
                var installPath = Path.Combine(_configPath, distroName);
                if (!await InstallDistroAsync(tarPath, distroName, ct))
                {
                    throw new InvalidOperationException($"Failed to install {distroName}");
                }

                // Configure the distro
                await ConfigureDistroAsync(distroName, ct);

                // Get the installed distro info
                var installedDistros = await GetInstalledDistrosAsync(ct);
                var installedDistro = installedDistros.FirstOrDefault(d => d.Name == distroName)
                    ?? new WslDistro
                    {
                        Name = distroName,
                        State = WslDistroState.Running,
                        Version = "2",
                        InstallPath = installPath
                    };

                return installedDistro;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to ensure distro {DistroName}", distroName);
                throw;
            }
        }

        /// <summary>
        /// Checks if a specific distribution exists.
        /// </summary>
        public async Task<bool> DistroExists(string distroName = "IIM-Ubuntu")
        {
            try
            {
                var result = await RunCommandAsync("wsl",
                    $"--list --quiet", 5000);

                if (result.ExitCode == 0)
                {
                    return result.StandardOutput.Contains(distroName, StringComparison.OrdinalIgnoreCase);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error checking if distro exists");
            }

            return false;
        }

        /// <summary>
        /// Installs a WSL distribution from a tar.gz file.
        /// </summary>
        public async Task<bool> InstallDistroAsync(string distroPath, string installName, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Installing distro {Name} from {Path}", installName, distroPath);

                var installPath = Path.Combine(_configPath, installName);
                Directory.CreateDirectory(installPath);

                var command = $"--import {installName} \"{installPath}\" \"{distroPath}\" --version 2";
                var result = await RunCommandAsync("wsl", command, 60000, ct);

                if (result.ExitCode == 0)
                {
                    _logger.LogInformation("Successfully installed {Name}", installName);

                    // Set as default if it's the Ubuntu distro
                    if (installName == UBUNTU_DISTRO)
                    {
                        await RunCommandAsync("wsl", $"--set-default {installName}", 5000, ct);
                    }

                    return true;
                }

                // Enhanced Error Message
                _logger.LogError(
                    "Failed to install WSL distro '{InstallName}'.\n" +
                    "ExitCode: {ExitCode}\n" +
                    "Command: wsl {Command}\n" +
                    "Install Path: {InstallPath}\n" +
                    "Distro Path: {DistroPath}\n" +
                    "StandardError: {StdErr}\n" +
                    "Hints: {Hints}",
                    installName,
                    result.ExitCode,
                    command,
                    installPath,
                    distroPath,
                    result.StandardError,
                    GetDistroImportHints(result.StandardError, distroPath)
                );

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while installing distro {Name}", installName);
                return false;
            }
        }

        /// <summary>
        /// Attempts to provide hints based on standard error output and context.
        /// </summary>
        private string GetDistroImportHints(string stdErr, string distroPath)
        {
            if (string.IsNullOrWhiteSpace(stdErr)) return "(No additional hints)";

            if (stdErr.Contains("The system cannot find the file specified", StringComparison.OrdinalIgnoreCase) ||
                !File.Exists(distroPath))
            {
                return "Check if the distro image exists and the path is correct. " +
                       "The download may have failed or the file may be corrupt.";
            }
            if (stdErr.Contains("invalid tar file", StringComparison.OrdinalIgnoreCase) ||
                stdErr.Contains("unexpected EOF", StringComparison.OrdinalIgnoreCase))
            {
                return "The downloaded file appears to be corrupt or incomplete. " +
                       "Try deleting it and re-downloading.";
            }
            if (stdErr.Contains("Access is denied", StringComparison.OrdinalIgnoreCase))
            {
                return "Access denied. Try running as administrator or check file/folder permissions.";
            }
            if (stdErr.Contains("not supported", StringComparison.OrdinalIgnoreCase))
            {
                return "Check if your WSL version supports this operation.";
            }
            if (stdErr.Contains("There is not enough space", StringComparison.OrdinalIgnoreCase))
            {
                return "Not enough disk space. Free up space and try again.";
            }

            return "(No specific hints. See standard error above.)";
        }

        /// <summary>
        /// Exports a WSL distribution to a tar file.
        /// </summary>
        public async Task<bool> ExportDistroAsync(string distroName, string exportPath, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Exporting {Distro} to {Path}", distroName, exportPath);

                var result = await RunCommandAsync("wsl",
                    $"--export {distroName} \"{exportPath}\"",
                    120000, ct); // 2 minute timeout for export

                if (result.ExitCode == 0)
                {
                    _logger.LogInformation("Successfully exported {Distro}", distroName);
                    return true;
                }

                _logger.LogError("Failed to export {Distro}: {Error}", distroName, result.StandardError);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export distro {Distro}", distroName);
                return false;
            }
        }

        /// <summary>
        /// Imports a WSL distribution from a tar file.
        /// </summary>
        public async Task<bool> ImportDistroAsync(string tarPath, string distroName, string installPath, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Importing {Distro} from {TarPath} to {InstallPath}",
                    distroName, tarPath, installPath);

                Directory.CreateDirectory(installPath);

                var result = await RunCommandAsync("wsl",
                    $"--import {distroName} \"{installPath}\" \"{tarPath}\" --version 2",
                    120000, ct); // 2 minute timeout for import

                if (result.ExitCode == 0)
                {
                    _logger.LogInformation("Successfully imported {Distro}", distroName);
                    return true;
                }

                _logger.LogError("Failed to import {Distro}: {Error}", distroName, result.StandardError);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import distro {Distro}", distroName);
                return false;
            }
        }

        /// <summary>
        /// Unregisters and removes a WSL distribution.
        /// </summary>
        public async Task<bool> RemoveDistroAsync(string distroName, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Removing distro {Distro}", distroName);

                var result = await RunCommandAsync("wsl",
                    $"--unregister {distroName}",
                    30000, ct);

                if (result.ExitCode == 0)
                {
                    // Remove installation directory if it exists
                    var installPath = Path.Combine(_configPath, distroName);
                    if (Directory.Exists(installPath))
                    {
                        try
                        {
                            Directory.Delete(installPath, true);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Could not delete install directory for {Distro}", distroName);
                        }
                    }

                    _logger.LogInformation("Successfully removed {Distro}", distroName);
                    return true;
                }

                _logger.LogError("Failed to remove {Distro}: {Error}", distroName, result.StandardError);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove distro {Distro}", distroName);
                return false;
            }
        }

        // ========================================
        // Private Distribution Helper Methods
        // ========================================

        /// <summary>
        /// Downloads a WSL distribution.
        /// </summary>
        private async Task<string?> DownloadDistroAsync(string downloadUrl, string distroName, CancellationToken ct)
        {
            try
            {
                var fileName = $"{distroName}.tar.gz";
                var downloadPath = Path.Combine(_configPath, "Downloads");
                Directory.CreateDirectory(downloadPath);

                var filePath = Path.Combine(downloadPath, fileName);

                // Check if already downloaded, but verify size
                if (File.Exists(filePath))
                {
                    var fileInfo = new FileInfo(filePath);
                    if (fileInfo.Length < 1024 * 1024) // Less than 1 MB
                    {
                        _logger.LogWarning("Existing download for {Path} is suspiciously small ({Size} bytes). Deleting and re-downloading.", filePath, fileInfo.Length);
                        try
                        {
                            File.Delete(filePath);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to delete suspiciously small file {Path}.", filePath);
                            throw; // You may choose to throw or return null here
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Using existing download at {Path}", filePath);
                        return filePath;
                    }
                }

                _logger.LogInformation("Downloading {Distro} from {Url}", distroName, downloadUrl);

                // Optional: Set a browser-like User-Agent to avoid weird redirects
                _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; IIMDownloader/1.0)");

                using var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);

                _logger.LogInformation("HTTP status: {Status}, headers: {Headers}", response.StatusCode, response.Headers.ToString());

                response.EnsureSuccessStatusCode();

                // Check content type - it should be a binary file, not text/html
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
                var contentLength = response.Content.Headers.ContentLength ?? 0;

                if (!contentType.StartsWith("application") && !contentType.Contains("gzip") && !contentType.Contains("x-tar"))
                {
                    var errorText = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Unexpected content type: {ContentType}, length: {ContentLength}, content: {Content}", contentType, contentLength, errorText.Substring(0, Math.Min(errorText.Length, 200)));
                    throw new InvalidOperationException($"Downloaded file is not a valid WSL image. Content-Type: {contentType}, Content-Length: {contentLength}");
                }

                using var contentStream = await response.Content.ReadAsStreamAsync(ct);
                using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

                var buffer = new byte[8192];
                long downloadedBytes = 0;
                var lastProgressReport = DateTime.UtcNow;

                int bytesRead;
                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead, ct);
                    downloadedBytes += bytesRead;

                    if ((DateTime.UtcNow - lastProgressReport).TotalSeconds >= 1)
                    {
                        var progress = contentLength > 0 ? (downloadedBytes * 100.0 / contentLength) : 0;
                        _logger.LogInformation("Download progress: {Progress:F1}% ({Downloaded}/{Total} bytes)",
                            progress, downloadedBytes, contentLength);
                        lastProgressReport = DateTime.UtcNow;
                    }
                }

                _logger.LogInformation("Download complete: {Path}, size: {Size} bytes", filePath, downloadedBytes);

                // Extra: If file is suspiciously small, log warning
                if (downloadedBytes < 1024 * 1024) // less than 1 MB
                {
                    _logger.LogWarning("Downloaded file size is suspiciously small: {Size} bytes. File: {Path}", downloadedBytes, filePath);
                }

                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download {Distro}", distroName);
                return null;
            }
            }


                /// <summary>
                /// Starts a WSL distribution.
                /// </summary>
        private async Task StartDistroAsync(string distroName, CancellationToken ct)
        {
            try
            {
                _logger.LogInformation("Starting distro {Distro}", distroName);

                // Start the distro with a simple command
                var result = await RunCommandAsync("wsl",
                    $"-d {distroName} -u root -- echo 'Starting distro'",
                    10000, ct);

                if (result.ExitCode == 0)
                {
                    _logger.LogInformation("Distro {Distro} started successfully", distroName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start distro {Distro}", distroName);
            }
        }

        /// <summary>
        /// Configures a newly installed distribution.
        /// </summary>
        private async Task ConfigureDistroAsync(string distroName, CancellationToken ct)
        {
            try
            {
                _logger.LogInformation("Configuring distro {Distro}", distroName);

                // Update package lists
                await ExecuteCommandAsync(distroName, "apt-get update", ct);

                // Install essential packages
                var essentialPackages = "curl wget git vim htop net-tools iputils-ping";
                await ExecuteCommandAsync(distroName,
                    $"apt-get install -y {essentialPackages}", ct);

                // Configure systemd if needed
                await ConfigureSystemdAsync(distroName, ct);

                // Set up IIM user
                await CreateIimUserAsync(distroName, ct);

                _logger.LogInformation("Distro {Distro} configured successfully", distroName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to configure distro {Distro}", distroName);
            }
        }

        /// <summary>
        /// Configures systemd in the distribution.
        /// </summary>
        private async Task ConfigureSystemdAsync(string distroName, CancellationToken ct)
        {
            var systemdScript = @"
                if [ ! -f /etc/wsl.conf ]; then
                    cat > /etc/wsl.conf << EOF
[boot]
systemd=true

[network]
generateHosts=true
generateResolvConf=true

[interop]
enabled=true
appendWindowsPath=true

[user]
default=iim
EOF
                fi
            ";

            await ExecuteCommandAsync(distroName, systemdScript, ct);
        }

        /// <summary>
        /// Creates the IIM user in the distribution.
        /// </summary>
        private async Task CreateIimUserAsync(string distroName, CancellationToken ct)
        {
            var userScript = @"
                if ! id -u iim >/dev/null 2>&1; then
                    useradd -m -s /bin/bash -G sudo,docker iim
                    echo 'iim ALL=(ALL) NOPASSWD:ALL' >> /etc/sudoers
                fi
            ";

            await ExecuteCommandAsync(distroName, userScript, ct);
        }
    }
}