using IIM.Shared.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces;

/// <summary>
/// Defines a common contract for managing the lifecycle of the appliance's backend services,
/// regardless of the underlying platform (WSL on Windows, Docker on Linux).
/// </summary>
public interface IApplianceServiceOrchestrator
{
    Task StartServicesAsync(IEnumerable<string> services);
    Task StopServicesAsync();
    Task<IEnumerable<ServiceStatus>> GetServicesStatusAsync();
    Task<ServiceStatus> GetServiceStatusAsync(string serviceName, CancellationToken ct = default);
    Task<bool> StartServiceAsync(string serviceName, ServiceConfig? config = null, CancellationToken ct = default);
    Task<bool> StopServiceAsync(string serviceName, CancellationToken ct = default);
    Task<bool> RestartServiceAsync(string serviceName, CancellationToken ct = default);
    Task<Dictionary<string, ServiceStatus>> GetAllServicesStatusAsync(CancellationToken ct = default);
    Task<bool> EnsureAllServicesAsync(CancellationToken ct = default);
}

