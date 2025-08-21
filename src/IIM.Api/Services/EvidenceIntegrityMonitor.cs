using IIM.Core.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IIM.Api.Services
{
    public class EvidenceIntegrityMonitor : BackgroundService
    {
        private readonly IEvidenceManager _evidenceManager;
        private readonly ILogger<EvidenceIntegrityMonitor> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(6);

        public EvidenceIntegrityMonitor(
            IEvidenceManager evidenceManager,
            ILogger<EvidenceIntegrityMonitor> logger)
        {
            _evidenceManager = evidenceManager;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Starting evidence integrity check");

                    // In production, get list of evidence IDs from database
                    // For now, this is a placeholder
                    var evidenceIds = new List<string>();

                    foreach (var evidenceId in evidenceIds)
                    {
                        try
                        {
                            var isValid = await _evidenceManager.VerifyIntegrityAsync(evidenceId, stoppingToken);

                            if (!isValid)
                            {
                                _logger.LogError("Integrity check failed for evidence {EvidenceId}", evidenceId);
                                // Send alert to administrators
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error checking evidence {EvidenceId}", evidenceId);
                        }
                    }

                    _logger.LogInformation("Evidence integrity check completed");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in integrity monitoring");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }
    }
}