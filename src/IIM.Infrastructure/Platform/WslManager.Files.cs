// ============================================
// File: src/IIM.Infrastructure/Platform/WslManager.Files.cs  
// Purpose: File sync and configuration management operations
// Author: IIM Platform Team
// Created: 2024
// ============================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IIM.Infrastructure.Platform.Models;
using Microsoft.Extensions.Logging;

namespace IIM.Infrastructure.Platform
{
    /// <summary>
    /// File operations and configuration management
    /// </summary>
    public sealed partial class WslManager
    {
        /// <summary>
        /// Synchronizes files from Windows to WSL.
        /// </summary>
        public async Task<bool> SyncFilesAsync(string windowsPath, string wslPath, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Syncing files from {WindowsPath} to {WslPath}", windowsPath, wslPath);

                // Convert Windows path to WSL path
                var wslWindowsPath = ConvertToWslPath(windowsPath);

                // Ensure target directory exists
                await ExecuteCommandAsync(UBUNTU_DISTRO, $"mkdir -p {wslPath}", ct);

                // Use rsync for incremental sync
                var command = $"rsync -av --progress {wslWindowsPath}/ {wslPath}/";
                var result = await ExecuteCommandAsync(UBUNTU_DISTRO, command, ct);

                if (result.ExitCode == 0)
                {
                    _logger.LogInformation("File sync completed successfully");
                    return true;
                }

                _logger.LogError("File sync failed: {Error}", result.StandardError);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync files");
                return false;
            }
        }

        /// <summary>
        /// Synchronizes files with advanced options.
        /// </summary>
        public async Task<bool> SyncFilesAsync(FileSyncConfig config, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Starting advanced file sync from {Source} to {Dest}",
                    config.WindowsPath, config.WslPath);

                var wslWindowsPath = ConvertToWslPath(config.WindowsPath);

                // Build rsync command with options
                var rsyncCmd = new StringBuilder("rsync ");

                // Basic options
                rsyncCmd.Append("-av ");

                if (config.DeleteOrphaned)
                    rsyncCmd.Append("--delete ");

                if (config.PreservePermissions)
                    rsyncCmd.Append("--perms ");

                // Include patterns
                foreach (var pattern in config.IncludePatterns)
                {
                    rsyncCmd.Append($"--include='{pattern}' ");
                }

                // Exclude patterns
                foreach (var pattern in config.ExcludePatterns)
                {
                    rsyncCmd.Append($"--exclude='{pattern}' ");
                }

                // Add progress if callback is provided
                if (config.ProgressCallback != null)
                {
                    rsyncCmd.Append("--progress ");
                }

                // Source and destination
                rsyncCmd.Append($"{wslWindowsPath}/ {config.WslPath}/");

                // Execute with streaming output for progress
                if (config.ProgressCallback != null)
                {
                    var exitCode = await ExecuteCommandWithStreamingAsync(
                        UBUNTU_DISTRO,
                        rsyncCmd.ToString(),
                        output => ParseRsyncProgress(output, config.ProgressCallback),
                        ct);

                    return exitCode == 0;
                }
                else
                {
                    var result = await ExecuteCommandAsync(UBUNTU_DISTRO, rsyncCmd.ToString(), ct);
                    return result.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Advanced file sync failed");
                return false;
            }
        }

        /// <summary>
        /// Copies a file from Windows to WSL.
        /// </summary>
        public async Task<bool> CopyFileToWslAsync(string windowsFilePath, string wslFilePath, CancellationToken ct = default)
        {
            try
            {
                if (!File.Exists(windowsFilePath))
                {
                    _logger.LogError("Source file does not exist: {Path}", windowsFilePath);
                    return false;
                }

                var wslWindowsPath = ConvertToWslPath(windowsFilePath);

                // Ensure target directory exists
                var wslDir = Path.GetDirectoryName(wslFilePath)?.Replace('\\', '/');
                if (!string.IsNullOrEmpty(wslDir))
                {
                    await ExecuteCommandAsync(UBUNTU_DISTRO, $"mkdir -p {wslDir}", ct);
                }

                // Copy file
                var result = await ExecuteCommandAsync(UBUNTU_DISTRO,
                    $"cp {wslWindowsPath} {wslFilePath}", ct);

                return result.ExitCode == 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to copy file to WSL");
                return false;
            }
        }

        /// <summary>
        /// Copies a file from WSL to Windows.
        /// </summary>
        public async Task<bool> CopyFileFromWslAsync(string wslFilePath, string windowsFilePath, CancellationToken ct = default)
        {
            try
            {
                // Check if file exists in WSL
                var checkResult = await ExecuteCommandAsync(UBUNTU_DISTRO,
                    $"test -f {wslFilePath} && echo 'exists'", ct);

                if (checkResult.ExitCode != 0)
                {
                    _logger.LogError("Source file does not exist in WSL: {Path}", wslFilePath);
                    return false;
                }

                // Ensure target directory exists
                var windowsDir = Path.GetDirectoryName(windowsFilePath);
                if (!string.IsNullOrEmpty(windowsDir))
                {
                    Directory.CreateDirectory(windowsDir);
                }

                // Copy file using WSL command
                var wslWindowsPath = ConvertToWslPath(windowsFilePath);
                var result = await ExecuteCommandAsync(UBUNTU_DISTRO,
                    $"cp {wslFilePath} {wslWindowsPath}", ct);

                return result.ExitCode == 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to copy file from WSL");
                return false;
            }
        }

        /// <summary>
        /// Cleans up temporary files and caches in WSL.
        /// </summary>
        public async Task<long> CleanupAsync(CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Starting WSL cleanup");
                long freedBytes = 0;

                // Clean apt cache
                var aptResult = await ExecuteCommandAsync(UBUNTU_DISTRO,
                    "du -sb /var/cache/apt/archives && apt-get clean && du -sb /var/cache/apt/archives", ct);

                if (aptResult.ExitCode == 0)
                {
                    var lines = aptResult.StandardOutput.Split('\n');
                    if (lines.Length >= 2)
                    {
                        var before = ParseSize(lines[0]);
                        var after = ParseSize(lines[1]);
                        freedBytes += before - after;
                    }
                }

                // Clean Docker images and containers
                var dockerCommands = new[]
                {
                    "docker container prune -f",
                    "docker image prune -f",
                    "docker volume prune -f",
                    "docker network prune -f"
                };

                foreach (var cmd in dockerCommands)
                {
                    var result = await ExecuteCommandAsync(UBUNTU_DISTRO, cmd, ct);
                    if (result.ExitCode == 0)
                    {
                        // Parse freed space from output
                        var match = System.Text.RegularExpressions.Regex.Match(
                            result.StandardOutput, @"Total reclaimed space: ([\d.]+)([GMK]B)");

                        if (match.Success)
                        {
                            var value = double.Parse(match.Groups[1].Value);
                            var unit = match.Groups[2].Value;
                            freedBytes += unit switch
                            {
                                "GB" => (long)(value * 1024 * 1024 * 1024),
                                "MB" => (long)(value * 1024 * 1024),
                                "KB" => (long)(value * 1024),
                                _ => 0
                            };
                        }
                    }
                }

                // Clean temporary files
                await ExecuteCommandAsync(UBUNTU_DISTRO, "rm -rf /tmp/*", ct);
                await ExecuteCommandAsync(UBUNTU_DISTRO, "rm -rf /var/tmp/*", ct);

                _logger.LogInformation("Cleanup completed. Freed {Bytes} bytes ({MB} MB)",
                    freedBytes, freedBytes / (1024 * 1024));

                return freedBytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cleanup failed");
                return 0;
            }
        }

        /// <summary>
        /// Gets the current WSL configuration.
        /// </summary>
        public async Task<Dictionary<string, string>> GetConfigurationAsync(CancellationToken ct = default)
        {
            var config = new Dictionary<string, string>();

            try
            {
                // Read .wslconfig from user profile
                var wslConfigPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".wslconfig");

                if (File.Exists(wslConfigPath))
                {
                    var lines = await File.ReadAllLinesAsync(wslConfigPath, ct);
                    foreach (var line in lines)
                    {
                        if (!line.StartsWith('#') && line.Contains('='))
                        {
                            var parts = line.Split('=', 2);
                            if (parts.Length == 2)
                            {
                                config[parts[0].Trim()] = parts[1].Trim();
                            }
                        }
                    }
                }

                // Read wsl.conf from distro
                var wslConfResult = await ExecuteCommandAsync(UBUNTU_DISTRO,
                    "cat /etc/wsl.conf 2>/dev/null", ct);

                if (wslConfResult.ExitCode == 0)
                {
                    var lines = wslConfResult.StandardOutput.Split('\n');
                    foreach (var line in lines)
                    {
                        if (!line.StartsWith('#') && line.Contains('='))
                        {
                            var parts = line.Split('=', 2);
                            if (parts.Length == 2)
                            {
                                config[$"wsl.conf.{parts[0].Trim()}"] = parts[1].Trim();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get configuration");
            }

            return config;
        }

        /// <summary>
        /// Updates WSL configuration settings.
        /// </summary>
        public async Task<bool> UpdateConfigurationAsync(Dictionary<string, string> settings, CancellationToken ct = default)
        {
            try
            {
                var wslConfigPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".wslconfig");

                var configLines = new List<string> { "[wsl2]" };

                foreach (var (key, value) in settings)
                {
                    if (!key.StartsWith("wsl.conf."))
                    {
                        configLines.Add($"{key}={value}");
                    }
                }

                await File.WriteAllLinesAsync(wslConfigPath, configLines, ct);

                // Update wsl.conf in distro
                var wslConfSettings = settings.Where(s => s.Key.StartsWith("wsl.conf."))
                    .ToDictionary(s => s.Key.Substring(9), s => s.Value);

                if (wslConfSettings.Any())
                {
                    var wslConfContent = new StringBuilder();
                    wslConfContent.AppendLine("[boot]");
                    wslConfContent.AppendLine("systemd=true");
                    wslConfContent.AppendLine();
                    wslConfContent.AppendLine("[network]");
                    wslConfContent.AppendLine("generateHosts=true");
                    wslConfContent.AppendLine("generateResolvConf=true");

                    foreach (var (key, value) in wslConfSettings)
                    {
                        wslConfContent.AppendLine($"{key}={value}");
                    }

                    var script = $"echo '{wslConfContent}' > /etc/wsl.conf";
                    await ExecuteCommandAsync(UBUNTU_DISTRO, script, ct);
                }

                _logger.LogInformation("Configuration updated successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update configuration");
                return false;
            }
        }

        /// <summary>
        /// Sets memory limit for WSL2.
        /// </summary>
        public async Task<bool> SetMemoryLimitAsync(int memoryGb, CancellationToken ct = default)
        {
            var settings = new Dictionary<string, string>
            {
                ["memory"] = $"{memoryGb}GB"
            };

            return await UpdateConfigurationAsync(settings, ct);
        }

        /// <summary>
        /// Sets CPU limit for WSL2.
        /// </summary>
        public async Task<bool> SetCpuLimitAsync(int cpuCount, CancellationToken ct = default)
        {
            var settings = new Dictionary<string, string>
            {
                ["processors"] = cpuCount.ToString()
            };

            return await UpdateConfigurationAsync(settings, ct);
        }

        // ========================================
        // Private File Helper Methods
        // ========================================

        /// <summary>
        /// Parses size from du command output.
        /// </summary>
        private long ParseSize(string duOutput)
        {
            var parts = duOutput.Split('\t');
            if (parts.Length > 0 && long.TryParse(parts[0], out var size))
            {
                return size;
            }
            return 0;
        }

        /// <summary>
        /// Parses rsync progress output.
        /// </summary>
        private void ParseRsyncProgress(string output, Action<FileSyncProgress> callback)
        {
            // Parse rsync progress output
            // Example: "     1,234,567  75%   12.34MB/s    0:00:05"
            var match = System.Text.RegularExpressions.Regex.Match(output,
                @"^\s*([\d,]+)\s+(\d+)%\s+([\d.]+[KMG]B/s)\s+([\d:]+)");

            if (match.Success)
            {
                var progress = new FileSyncProgress
                {
                    BytesTransferred = long.Parse(match.Groups[1].Value.Replace(",", "")),
                    ProgressPercent = double.Parse(match.Groups[2].Value),
                    CurrentFile = output // You might parse filename from other lines
                };

                // Parse speed
                var speedStr = match.Groups[3].Value;
                var speedMatch = System.Text.RegularExpressions.Regex.Match(speedStr, @"([\d.]+)([KMG])B/s");
                if (speedMatch.Success)
                {
                    var value = double.Parse(speedMatch.Groups[1].Value);
                    progress.SpeedBps = speedMatch.Groups[2].Value switch
                    {
                        "G" => value * 1024 * 1024 * 1024,
                        "M" => value * 1024 * 1024,
                        "K" => value * 1024,
                        _ => value
                    };
                }

                // Parse time remaining
                var timeStr = match.Groups[4].Value;
                if (TimeSpan.TryParse(timeStr, out var remaining))
                {
                    progress.EstimatedTimeRemaining = remaining;
                }

                callback(progress);
            }
        }
    }
}