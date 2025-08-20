using IIM.Core.Mediator;
using IIM.Infrastructure.Platform;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Application.Commands.Wsl
{
    /// <summary>
    /// Fixed handler for ensuring WSL2 is properly configured
    /// </summary>
    public class EnsureWslCommandHandler : IRequestHandler<EnsureWslCommand, Unit>
    {
        private readonly IWslManager _wslManager;
        private readonly IWslServiceOrchestrator _serviceOrchestrator;
        private readonly IMediator _mediator;
        private readonly ILogger<EnsureWslCommandHandler> _logger;

        public EnsureWslCommandHandler(
            IWslManager wslManager,
            IWslServiceOrchestrator serviceOrchestrator,
            IMediator mediator,
            ILogger<EnsureWslCommandHandler> logger)
        {
            _wslManager = wslManager;
            _serviceOrchestrator = serviceOrchestrator;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<Unit> Handle(EnsureWslCommand request, CancellationToken cancellationToken)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(request.Timeout);

            try
            {
                _logger.LogInformation("Starting WSL2 setup process");

                // Check current WSL status
                var status = await _wslManager.GetStatusAsync(cts.Token);
                _logger.LogInformation("WSL Status: Installed={Installed}, WSL2={IsWsl2}, Ready={Ready}",
                    status.IsInstalled, status.IsWsl2, status.IsReady);

                // Install WSL if needed
                if (!status.IsInstalled && request.AutoInstall)
                {
                    _logger.LogInformation("WSL2 not installed, beginning installation");

                    if (!await _wslManager.IsWslEnabled())
                    {
                        _logger.LogInformation("Enabling WSL feature");
                        var enableResult = await _wslManager.EnableWsl();

                        if (!enableResult)
                        {
                            throw new InvalidOperationException("Failed to enable WSL feature");
                        }

                        await _mediator.Publish(new WslFeatureEnabledNotification
                        {
                            Timestamp = DateTimeOffset.UtcNow,
                            RequiresRestart = true
                        }, cts.Token);
                    }
                }

                // Install distro if needed
                if (request.InstallDistro && !status.HasIimDistro)
                {
                    _logger.LogInformation("Installing IIM Ubuntu distribution");

                    var distro = await _wslManager.EnsureDistroAsync(request.DistroName, cts.Token);

                    if (distro == null)
                    {
                        throw new InvalidOperationException($"Failed to install {request.DistroName}");
                    }

                    _logger.LogInformation("Distribution {DistroName} installed", distro.Name);

                    await _mediator.Publish(new WslDistroInstalledNotification
                    {
                        DistroName = distro.Name,
                        State = distro.State,
                        Version = distro.Version,
                        Timestamp = DateTimeOffset.UtcNow
                    }, cts.Token);
                }

                // Start services if requested
                if (request.StartServices)
                {
                    _logger.LogInformation("Starting required services");

                    var allStarted = await _serviceOrchestrator.EnsureAllServicesAsync(cts.Token);

                    if (!allStarted)
                    {
                        _logger.LogWarning("Some services failed to start");

                        var serviceStatuses = await _serviceOrchestrator.GetAllServicesStatusAsync(cts.Token);

                        foreach (var requiredService in request.RequiredServices)
                        {
                            if (serviceStatuses.TryGetValue(requiredService, out var serviceStatus))
                            {
                                // Check if service is healthy (ServiceStatus doesn't have IsRunning)
                                if (!serviceStatus.IsHealthy)
                                {
                                    _logger.LogWarning("Service {Service} is not healthy", requiredService);
                                    await _serviceOrchestrator.RestartServiceAsync(requiredService, cts.Token);
                                }
                            }
                        }
                    }
                }

                // Final health check
                var healthCheck = await _wslManager.HealthCheckAsync(cts.Token);

                if (!healthCheck.IsHealthy)
                {
                    _logger.LogError("WSL health check failed: {Issues}",
                        string.Join(", ", healthCheck.Issues));

                    throw new InvalidOperationException(
                        $"WSL setup completed but health check failed: {string.Join(", ", healthCheck.Issues)}");
                }

                _logger.LogInformation("WSL2 setup completed successfully");

                await _mediator.Publish(new WslSetupCompletedNotification
                {
                    Success = true,
                    DistroName = request.DistroName,
                    ServicesStarted = request.RequiredServices,
                    Timestamp = DateTimeOffset.UtcNow
                }, cts.Token);

                return Unit.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to ensure WSL2 is configured");

                await _mediator.Publish(new WslSetupFailedNotification
                {
                    Error = ex.Message,
                    Timestamp = DateTimeOffset.UtcNow
                }, cancellationToken);

                throw;
            }
        }
    }
}