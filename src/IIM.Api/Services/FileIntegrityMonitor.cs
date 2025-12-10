using IIM.Core.Services;
using IIM.Shared.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IIM.Api.Services
{
    public class FileIntegrityMonitor : BackgroundService
    {
        private readonly IFileStore  _fileManager;
        private readonly ILogger<FileIntegrityMonitor> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(6);

        public FileIntegrityMonitor(
			IFileStore fileManager,
            ILogger<FileIntegrityMonitor> logger)
        {
            _fileManager = fileManager;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Starting file integrity check");

                    // In production, get list of evidence IDs from database
                    // For now, this is a placeholder
                    var fileIds = new List<string>();

                    foreach (var fileId in fileIds)
                    {
                        try
                        {
                            //var isValid = await _fileManager.VerifyIntegrityAsync(fileId, stoppingToken);

                            //if (!isValid)
                            //{
                            //    _logger.LogError("Integrity check failed for evidence {EvidenceId}", evidenceId);
                            //    // Send alert to administrators
                            //}
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error checking file {EvidenceId}", fileId);
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