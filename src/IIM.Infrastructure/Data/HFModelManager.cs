
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using System.Collections.Concurrent;

namespace IIM.Infrastructure.Data
{
    public class ModelManager : IModelManager
    {
        private readonly ModelHubDbContext _db;
        private readonly IModelDownloader _downloader;
        private readonly IProgressNotifier _progressNotifier;
        private readonly IAuditLogger _auditLogger;

        // Simple in-memory queue for demonstration
        private static readonly ConcurrentDictionary<string, CancellationTokenSource> ActiveDownloads = new();

        public ModelManager(
            ModelHubDbContext db,
            IModelDownloader downloader,
            IProgressNotifier progressNotifier,
            IAuditLogger auditLogger)
        {
            _db = db;
            _downloader = downloader;
            _progressNotifier = progressNotifier;
            _auditLogger = auditLogger;
        }

        public async Task<List<ModelInfo>> ListModelsAsync()
        {
            var models = await _db.Models.AsNoTracking().ToListAsync();
            return models.Select(e => new ModelInfo
            {
                Id = e.ModelId,
                Name = e.Name,
                Status = e.Status,
                DownloadedAt = e.DownloadedAt,
                Message = e.Message
            }).ToList();
        }

        public async Task EnqueueDownloadAsync(string modelId, string connectionId)
        {
            // Prevent duplicate downloads
            var existing = await _db.Models.FirstOrDefaultAsync(m => m.ModelId == modelId);
            if (existing != null && (existing.Status == "Downloading" || existing.Status == "Queued"))
                return;

            // Mark as Queued
            if (existing == null)
            {
                existing = new ModelInfo
                {
                    Id = modelId,
                    Name = modelId,
                    Status = "Queued",
                    DownloadedAt = null,
                    Message = "Queued"
                };
                _db.Models.Add(existing);
            }
            else
            {
                existing.Status = "Queued";
                existing.Message = "Queued";
            }
            await _db.SaveChangesAsync();

            // Start download (fire and forget)
            _ = Task.Run(async () =>
            {
                var cts = new CancellationTokenSource();
                ActiveDownloads[modelId] = cts;
                await DownloadAndTrackAsync(modelId, connectionId, cts.Token);
                ActiveDownloads.TryRemove(modelId, out _);
            });
        }

        private async Task DownloadAndTrackAsync(string modelId, string connectionId, CancellationToken token)
        {
            var entity = await _db.Models.FirstOrDefaultAsync(m => m.ModelId == modelId);
            try
            {
                entity.Status = "Downloading";
                entity.Message = "Downloading";
                await _db.SaveChangesAsync();

                // Update progress callback
                async Task OnProgress(int percent, string msg)
                {
                    entity.Message = msg;
                    await _progressNotifier.NotifyProgressAsync(connectionId, modelId, msg, percent);
                    await _db.SaveChangesAsync();
                }

                string targetPath = Path.Combine("DownloadedModels", modelId.Replace('/', '_'));
                await _downloader.DownloadModelAsync(modelId, targetPath, OnProgress, token);

                entity.Status = "Available";
                entity.DownloadedAt = DateTime.UtcNow;
                entity.Message = "Download complete";
                await _auditLogger.LogAsync(new AuditEvent
                {
                    Timestamp = DateTime.UtcNow,
                    Action = "Download",
                    ModelId = modelId,
                    Details = "Completed"
                });
            }
            catch (OperationCanceledException)
            {
                entity.Status = "Canceled";
                entity.Message = "Canceled by user";
                await _auditLogger.LogAsync(new AuditEvent
                {
                    Timestamp = DateTime.UtcNow,
                    Action = "Cancel",
                    ModelId = modelId,
                    Details = "Canceled by user"
                });
            }
            catch (Exception ex)
            {
                entity.Status = "Failed";
                entity.Message = $"Failed: {ex.Message}";
                await _auditLogger.LogAsync(new AuditEvent
                {
                    Timestamp = DateTime.UtcNow,
                    Action = "DownloadError",
                    ModelId = modelId,
                    Details = ex.ToString()
                });
            }
            await _db.SaveChangesAsync();
            await _progressNotifier.NotifyProgressAsync(connectionId, modelId, entity.Message, 100);
        }

        public async Task CancelDownloadAsync(string modelId, string connectionId)
        {
            if (ActiveDownloads.TryGetValue(modelId, out var cts))
                cts.Cancel();
            var entity = await _db.Models.FirstOrDefaultAsync(m => m.ModelId == modelId);
            if (entity != null)
            {
                entity.Status = "Canceled";
                entity.Message = "Canceled by user";
                await _db.SaveChangesAsync();
            }
        }

        public async Task DeleteModelAsync(string modelId)
        {
            var entity = await _db.Models.FirstOrDefaultAsync(m => m.ModelId == modelId);
            if (entity != null)
            {
                _db.Models.Remove(entity);
                await _db.SaveChangesAsync();
            }
            // Delete files
            var folder = Path.Combine("DownloadedModels", modelId.Replace('/', '_'));
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);

            await _auditLogger.LogAsync(new AuditEvent
            {
                Timestamp = DateTime.UtcNow,
                Action = "Delete",
                ModelId = modelId,
                Details = "Deleted model and files"
            });
        }

        public async Task RefreshModelAsync(string modelId)
        {
            await DeleteModelAsync(modelId);
            // Could call EnqueueDownloadAsync if desired
        }

        public async Task<List<AuditEvent>> GetAuditLogsAsync()
        {
            return await _db.AuditLogs.Select(x => new AuditEvent
            {
                Timestamp = x.Timestamp,
                Action = x.Action,
                ModelId = x.ModelId,
                Details = x.Details
            }).ToListAsync();
        }
    }
}