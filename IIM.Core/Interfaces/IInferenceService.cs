using System;
using System.Threading.Tasks;
using IIM.Core.Models;

namespace IIM.Core.Interfaces
{
    /// <summary>
    /// Interface for AI inference operations
    /// Implemented by MockInferenceService (dev) and GpuInferenceService (prod)
    /// </summary>
    public interface IInferenceService
    {
        Task<TranscriptionResult> TranscribeAudioAsync(string audioPath, string language = "en");
        Task<ImageSearchResults> SearchImagesAsync(byte[] imageData, int topK = 5);
        Task<RagResponse> QueryDocumentsAsync(string query, string collection = "default");
        Task<bool> IsGpuAvailable();
        Task<DeviceInfo> GetDeviceInfo();
    }
}
