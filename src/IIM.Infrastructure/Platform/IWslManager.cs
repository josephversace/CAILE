// ============================================
// File: src/IIM.Infrastructure/Platform/WslManager.cs
// Purpose: Complete WSL2 management with DirectML support for AMD Strix
// Author: IIM Platform Team
// Created: 2024
// ============================================


namespace IIM.Infrastructure.Platform
{
    public interface IWslManager
    {
        Task<bool> DistroExists(string distroName = "IIM-Ubuntu");
        Task<bool> EnableWsl();
        Task<WslDistro> EnsureDistroAsync(string distroName = "IIM-Ubuntu", CancellationToken ct = default);
        Task<WslNetworkInfo> GetNetworkInfoAsync(string distroName, CancellationToken ct = default);
        Task<WslStatus> GetStatusAsync(CancellationToken ct = default);
        Task<HealthCheckResult> HealthCheckAsync(CancellationToken ct = default);
        Task<bool> InstallDistroAsync(string distroPath, string installName, CancellationToken ct = default);
        Task<bool> IsWslEnabled();
        Task<bool> StartIim();
        Task<bool> StartServicesAsync(WslDistro distro, CancellationToken ct = default);
        Task<bool> SyncFilesAsync(string windowsPath, string wslPath, CancellationToken ct = default);


            /// Installs Tor and applies the proxy environment settings in the WSL environment.
            /// </summary>
            /// <param name="windowsProxyPath">Path to the proxy.env file on the Windows filesystem.</param>
            /// <param name="cancellationToken">Cancellation token.</param>
            Task InstallTorAndApplyProxyAsync(string windowsProxyPath, CancellationToken cancellationToken);
   
    }
}