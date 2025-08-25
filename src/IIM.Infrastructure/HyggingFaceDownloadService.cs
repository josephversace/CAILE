using IIM.Infrastructure.Storage;
using IIM.Shared.Interfaces;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;


namespace IIM.Infrastructure.Services
{
    public class HuggingFaceModelDownloader : IModelDownloader
    {
        private readonly StorageConfiguration _storage;
        private readonly IConfigurationService _config;
        private readonly IHttpClientFactory _httpClientFactory;

        public HuggingFaceModelDownloader(StorageConfiguration storage, IConfigurationService config, IHttpClientFactory factory)
        {
            _storage = storage;
            _config = config;
            _httpClientFactory = factory;
        }

        public async Task DownloadModelAsync(string modelId, string targetPath, Func<int, string, Task> onProgress, CancellationToken cancellationToken)
        {

            var httpClient = _httpClientFactory.CreateClient();

            string path = _storage.SystemModelsPath;


            // List of possible model file names, in order of preference
            var possibleModelFiles = new[]
            {
        "model-q4_k.gguf",       // Most common GGUF
        "model.gguf",            // Generic GGUF
        "model.onnx",            // Standard ONNX
        "model-quantized.onnx"  // Quantized ONNX variant
       
    };

            string modelFileDownloaded = null;

            foreach (var fileName in possibleModelFiles)
            {
                var fileUrl = $"https://huggingface.co/{modelId}/resolve/main/{fileName}";
                var filePath = Path.Combine(targetPath, fileName);

                try
                {
                    using var resp = await httpClient.GetAsync(fileUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                    if (resp.IsSuccessStatusCode)
                    {
                        using var fs = System.IO.File.Create(filePath);
                        await resp.Content.CopyToAsync(fs, cancellationToken);
                        modelFileDownloaded = fileName;
                        await onProgress(50, $"{fileName} downloaded.");
                        break;
                    }
                }
                catch { /* swallow and try next file */ }
            }

            if (modelFileDownloaded == null)
            {
                await onProgress(50, "No model file found (GGUF, ONNX, or bin).");
                // Optionally throw or handle as needed
            }
            else
            {


                string readmeUrl = $"https://huggingface.co/{modelId}/raw/main/README.md";
                string readmePath = Path.Combine(targetPath, "README.md");


                string configUrl = $"https://huggingface.co/{modelId}/resolve/main/config.json";
                string configPath = Path.Combine(targetPath, "config.json");

                try
                {
                    string config = await httpClient.GetStringAsync(configUrl, cancellationToken);
                    System.IO.File.WriteAllText(configPath, config);
                    await onProgress(90, "config.json downloaded.");
                }
                catch
                {
                    await onProgress(90, "config.json not found.");
                }


                await onProgress(100, "Download complete.");
            }
        }
    }
}
