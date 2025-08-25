
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
    /// Service interface for managing model metadata
    /// </summary>
    public interface IModelConfigurationService
    {
        Task<ModelConfiguration> GetMetadataAsync(string modelId, CancellationToken ct = default);
        Task RegisterMetadataAsync(ModelConfiguration metadata, CancellationToken ct = default);
        Task<List<ModelConfiguration>> GetAllMetadataAsync(CancellationToken ct = default);
        Task LoadFromConfigurationAsync(CancellationToken ct = default);
    }

}
