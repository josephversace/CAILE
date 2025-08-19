using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IIM.Core.AI;
using IIM.Core.Inference;
using IIM.Core.Models;
using IIM.Shared.Enums;
using Microsoft.Extensions.Logging;

namespace IIM.Application.Interfaces
{
    /// <summary>
    /// Interface for AI inference operations supporting multiple model types
    /// </summary>
    public interface IInferenceService
    {
        // Existing methods
        Task<string> InferAsync(string prompt, CancellationToken cancellationToken = default);
        Task<T> GenerateAsync<T>(string modelId, object input, CancellationToken cancellationToken = default);

        // Audio transcription - uses existing TranscriptionResult from Models/Analysis.cs
        Task<TranscriptionResult> TranscribeAudioAsync(string audioPath, string language = "en", CancellationToken cancellationToken = default);

        // Image search - returns ImageSearchResults to match Investigation.razor expectations
        Task<ImageSearchResults> SearchImagesAsync(byte[] imageData, int topK = 5, CancellationToken cancellationToken = default);

        // Device information - uses existing DeviceInfo from Models/InferenceModels.cs
        Task<DeviceInfo> GetDeviceInfo();
        Task<bool> IsGpuAvailable();

        // RAG pipeline - uses existing RagResponse from Models/InferenceModels.cs
        Task<RagResponse> QueryDocumentsAsync(string query, string collection = "default", CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Production implementation of inference service
    /// </summary>
 
}