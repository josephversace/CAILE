// ============================================
// File: src/IIM.Core/Services/ModelManagementService.cs
// Purpose: Service for managing AI models and GPU resources
// ============================================

using IIM.Core.AI;
using IIM.Core.Models;
using IIM.Core.RAG;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Core.Services
{
    /// <summary>
    /// Service that manages AI models and GPU resources.
    /// Wraps IModelOrchestrator for simplified access.
    /// </summary>
    public interface IModelManagementService
    {
 
        Task<List<ModelConfiguration>> GetAvailableModelsAsync(CancellationToken cancellationToken = default);
        Task<bool> LoadModelAsync(string modelId, CancellationToken cancellationToken = default);
        Task<bool> UnloadModelAsync(string modelId, CancellationToken cancellationToken = default);
        Task<GpuStats> GetGpuStatsAsync(CancellationToken cancellationToken = default);
    }

    public class ModelManagementService : IModelManagementService
    {
        private readonly IModelOrchestrator _modelOrchestrator;
        private readonly ILogger<ModelManagementService> _logger;

        public ModelManagementService(IModelOrchestrator modelOrchestrator, ILogger<ModelManagementService> logger)
        {
            _modelOrchestrator = modelOrchestrator;
            _logger = logger;
        }

        public async Task<List<ModelConfiguration>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
        {
            return await _modelOrchestrator.GetAvailableModelsAsync(cancellationToken);
        }

        public async Task<bool> LoadModelAsync(string modelId, CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new ModelRequest { ModelId = modelId };
                await _modelOrchestrator.LoadModelAsync(request, null, cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load model {ModelId}", modelId);
                return false;
            }
        }

        public async Task<bool> UnloadModelAsync(string modelId, CancellationToken cancellationToken = default)
        {
            return await _modelOrchestrator.UnloadModelAsync(modelId, cancellationToken);
        }

        public async Task<GpuStats> GetGpuStatsAsync(CancellationToken cancellationToken = default)
        {
            return await _modelOrchestrator.GetGpuStatsAsync(cancellationToken);
        }
    }
}