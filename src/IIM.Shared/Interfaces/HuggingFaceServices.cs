using IIM.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces
{



            public interface IModelDownloader
            {
                Task DownloadModelAsync(string modelId, string targetPath,
                    Func<int, string, Task> onProgress, CancellationToken cancellationToken);
            }


            public interface IModelManager
            {
                Task<List<ModelInfo>> ListModelsAsync();
                Task EnqueueDownloadAsync(string modelId, string connectionId);
                Task CancelDownloadAsync(string modelId, string connectionId);
                Task DeleteModelAsync(string modelId);
                Task RefreshModelAsync(string modelId);
                Task<List<AuditEvent>> GetAuditLogsAsync();
            }



}
