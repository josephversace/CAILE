using IIM.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces
{
    /// <summary>
    /// Interface for orchestrating services within WSL2
    /// </summary>
    public interface IWslServiceOrchestrator
    {
        Task<ServiceStatus> GetServiceStatusAsync(string serviceName, CancellationToken ct = default);
        Task<bool> StartServiceAsync(string serviceName, ServiceConfig? config = null, CancellationToken ct = default);
        Task<bool> StopServiceAsync(string serviceName, CancellationToken ct = default);
        Task<bool> RestartServiceAsync(string serviceName, CancellationToken ct = default);
        Task<Dictionary<string, ServiceStatus>> GetAllServicesStatusAsync(CancellationToken ct = default);
        Task<bool> EnsureAllServicesAsync(CancellationToken ct = default);
    }

}
