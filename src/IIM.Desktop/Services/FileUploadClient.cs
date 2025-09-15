using IIM.Shared.Interfaces;
using IIM.Shared.Models.Core;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Desktop.Services
{
    public class FileUploadClient
    {
        private readonly ILogger<FileUploadClient> _logger;
        private readonly IIIMApiClient _apiClient;
        private readonly HttpClient _httpClient;

        public FileUploadClient(
            ILogger<FileUploadClient> logger,
            IIIMApiClient apiClient,
            HttpClient httpClient)
        {
            _logger = logger;
            _apiClient = apiClient;
            _httpClient = httpClient;
        }

        public async Task<VirtualFile?> UploadFileAsync(
            string filePath,
            Guid workspaceId,
            string virtualPath,
            IProgress<UploadProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var fileName = Path.GetFileName(filePath);
                var fileInfo = new FileInfo(filePath);

                progress?.Report(new UploadProgress { Status = "Computing file hash..." });
                var fileHash = await ComputeFileHashAsync(filePath, cancellationToken);
                _logger.LogInformation("Computed hash {Hash} for file {FileName}", fileHash, fileName);

                progress?.Report(new UploadProgress { Status = "Initiating upload...", Percentage = 10 });

                var initiateResponse = await _apiClient.InitiateFileUploadAsync(
                    workspaceId,
                    virtualPath,
                    fileName,
                    fileInfo.Length,
                    fileHash,
                    cancellationToken);

                if (initiateResponse.IsDuplicate)
                {
                    _logger.LogInformation("File is a duplicate of an existing file. No upload needed.");
                    progress?.Report(new UploadProgress { Status = "File already exists. Link created.", Percentage = 100, IsDuplicate = true });
                    return initiateResponse.VirtualFile;
                }

                if (string.IsNullOrEmpty(initiateResponse.UploadUrl))
                {
                    throw new InvalidOperationException("API did not provide an upload URL.");
                }

                progress?.Report(new UploadProgress { Status = "Uploading file...", Percentage = 20 });

                await UploadToPresignedUrlAsync(
                    filePath,
                    initiateResponse.UploadUrl,
                    cancellationToken);

                progress?.Report(new UploadProgress { Status = "Verifying upload...", Percentage = 90 });

                var confirmedFile = await _apiClient.ConfirmFileUploadAsync(initiateResponse.TransactionId, cancellationToken);

                if (confirmedFile == null)
                {
                    throw new InvalidOperationException("Upload confirmation failed.");
                }

                progress?.Report(new UploadProgress { Status = "Upload complete!", Percentage = 100 });
                return confirmedFile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload file {FilePath}", filePath);
                progress?.Report(new UploadProgress { Status = $"Upload failed: {ex.Message}", HasError = true });
                throw;
            }
        }

        private async Task<string> ComputeFileHashAsync(string filePath, CancellationToken cancellationToken)
        {
            using var stream = File.OpenRead(filePath);
            using var sha256 = SHA256.Create();
            var hashBytes = await sha256.ComputeHashAsync(stream, cancellationToken);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        private async Task UploadToPresignedUrlAsync(
            string filePath,
            string uploadUrl,
            CancellationToken cancellationToken)
        {
            using var fileStream = File.OpenRead(filePath);
            using var content = new StreamContent(fileStream);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

            var response = await _httpClient.PutAsync(uploadUrl, content, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
    }

    public class UploadProgress
    {
        public string Status { get; set; } = string.Empty;
        public int Percentage { get; set; }
        public long BytesTransferred { get; set; }
        public long TotalBytes { get; set; }
        public bool IsDuplicate { get; set; }
        public bool HasError { get; set; }
    }
}

