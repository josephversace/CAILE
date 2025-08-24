using IIM.Desktop.Services.Http;

using IIM.Shared.Interfaces;

using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Desktop.Services
{
    /// <summary>
    /// Client-side service for handling evidence uploads
    /// </summary>
    public class EvidenceUploadClient
    {
        private readonly ILogger<EvidenceUploadClient> _logger;
        private readonly IIIMApiClient _apiClient;
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Initializes the evidence upload client
        /// </summary>
        /// <param name="logger">Logger for diagnostics</param>
        /// <param name="apiClient">API client for backend communication</param>
        /// <param name="httpClient">HTTP client for direct MinIO uploads</param>
        public EvidenceUploadClient(
            ILogger<EvidenceUploadClient> logger,
            IIIMApiClient apiClient,
            HttpClient httpClient)
        {
            _logger = logger;
            _apiClient = apiClient;
            _httpClient = httpClient;
        }

        /// <summary>
        /// Uploads evidence file with deduplication check
        /// </summary>
        /// <param name="filePath">Path to the file to upload</param>
        /// <param name="metadata">Evidence metadata</param>
        /// <param name="progress">Progress reporter for upload status</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Evidence object if successful, null if duplicate</returns>
        public async Task<Evidence?> UploadEvidenceAsync(
            string filePath,
            EvidenceMetadata metadata,
            IProgress<UploadProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var fileName = Path.GetFileName(filePath);
                var fileInfo = new FileInfo(filePath);

                // Step 1: Compute file hash on client
                progress?.Report(new UploadProgress
                {
                    Status = "Computing file hash...",
                    Percentage = 0
                });

                var fileHash = await ComputeFileHashAsync(filePath, cancellationToken);

                _logger.LogInformation(
                    "Computed hash {Hash} for file {FileName}",
                    fileHash, fileName);

                // Step 2: Initiate upload with API
                progress?.Report(new UploadProgress
                {
                    Status = "Checking for duplicates...",
                    Percentage = 10
                });

                var initiateRequest = new InitiateEvidenceUploadRequest
                {
                    FileHash = fileHash,
                    FileName = fileName,
                    FileSize = fileInfo.Length,
                    ContentType = GetContentType(fileName),
                    Metadata = metadata
                };

                var initiateResponse = await _apiClient.InitiateEvidenceUploadAsync(
                    initiateRequest,
                    cancellationToken);

                // Step 3: Handle duplicate
                if (initiateResponse.Status == EvidenceUploadStatus.Duplicate)
                {
                    _logger.LogInformation(
                        "File is duplicate of evidence {EvidenceId}",
                        initiateResponse.DuplicateEvidenceId);

                    progress?.Report(new UploadProgress
                    {
                        Status = $"File already exists (Evidence ID: {initiateResponse.DuplicateEvidenceId})",
                        Percentage = 100,
                        IsDuplicate = true
                    });

                    return null; // Or return the existing evidence reference
                }

                // Step 4: Upload to MinIO using pre-signed URL
                if (string.IsNullOrEmpty(initiateResponse.UploadUrl))
                {
                    throw new InvalidOperationException("No upload URL provided");
                }

                progress?.Report(new UploadProgress
                {
                    Status = "Uploading file...",
                    Percentage = 20
                });

                await UploadToMinIOAsync(
                    filePath,
                    initiateResponse.UploadUrl,
                    initiateResponse.RequiredHeaders,
                    progress,
                    cancellationToken);

                // Step 5: Confirm upload completion
                progress?.Report(new UploadProgress
                {
                    Status = "Verifying upload...",
                    Percentage = 90
                });

                var confirmRequest = new ConfirmEvidenceUploadRequest
                {
                    EvidenceId = initiateResponse.EvidenceId,
                    ClientHash = fileHash
                };

                var confirmResponse = await _apiClient.ConfirmEvidenceUploadAsync(
                    confirmRequest,
                    cancellationToken);

                if (!confirmResponse.Success)
                {
                    throw new InvalidOperationException(
                        $"Upload verification failed: {confirmResponse.ErrorMessage}");
                }

                progress?.Report(new UploadProgress
                {
                    Status = "Upload complete",
                    Percentage = 100
                });

                // Return the evidence object
                return new Evidence
                {
                    Id = initiateResponse.EvidenceId,
                    OriginalFileName = fileName,
                    Hash = fileHash,
                    FileSize = fileInfo.Length,
                    Metadata = metadata,
                    Status = confirmResponse.Status
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload evidence file {FilePath}", filePath);

                progress?.Report(new UploadProgress
                {
                    Status = $"Upload failed: {ex.Message}",
                    Percentage = 0,
                    HasError = true
                });

                throw;
            }
        }

        /// <summary>
        /// Computes SHA-256 hash of a file
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Hex string of SHA-256 hash</returns>
        private async Task<string> ComputeFileHashAsync(
            string filePath,
            CancellationToken cancellationToken)
        {
            using var stream = File.OpenRead(filePath);
            using var sha256 = SHA256.Create();

            var hashBytes = await sha256.ComputeHashAsync(stream, cancellationToken);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>
        /// Uploads file directly to MinIO using pre-signed URL
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <param name="uploadUrl">Pre-signed upload URL</param>
        /// <param name="headers">Required headers for upload</param>
        /// <param name="progress">Progress reporter</param>
        /// <param name="cancellationToken">Cancellation token</param>
        private async Task UploadToMinIOAsync(
            string filePath,
            string uploadUrl,
            Dictionary<string, string>? headers,
            IProgress<UploadProgress>? progress,
            CancellationToken cancellationToken)
        {
            using var fileStream = File.OpenRead(filePath);
            using var content = new StreamContent(fileStream);

            // Add required headers
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            // Create progress wrapper for upload
            var progressHandler = new SimpleProgressHandler();
            progressHandler.HttpSendProgress += (_, args) =>
            {
                var percentage = 20 + (int)(70.0 * args.ProgressPercentage / 100);
                progress?.Report(new UploadProgress
                {
                    Status = "Uploading file...",
                    Percentage = percentage,
                    BytesTransferred = args.BytesTransferred,
                    TotalBytes = args.TotalBytes ?? 0
                });
            };

            using var progressClient = new HttpClient(progressHandler);

            var response = await progressClient.PutAsync(
                uploadUrl,
                content,
                cancellationToken);

            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Determines content type from file extension
        /// </summary>
        /// <param name="fileName">Name of the file</param>
        /// <returns>MIME type string</returns>
        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName)?.ToLowerInvariant();

            return extension switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".txt" => "text/plain",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".mp4" => "video/mp4",
                ".mp3" => "audio/mpeg",
                _ => "application/octet-stream"
            };
        }
    }

    /// <summary>
    /// Progress information for file upload
    /// </summary>
    public class UploadProgress
    {
        /// <summary>
        /// Current status message
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Percentage complete (0-100)
        /// </summary>
        public int Percentage { get; set; }

        /// <summary>
        /// Bytes transferred so far
        /// </summary>
        public long BytesTransferred { get; set; }

        /// <summary>
        /// Total bytes to transfer
        /// </summary>
        public long TotalBytes { get; set; }

        /// <summary>
        /// Whether this is a duplicate file
        /// </summary>
        public bool IsDuplicate { get; set; }

        /// <summary>
        /// Whether an error occurred
        /// </summary>
        public bool HasError { get; set; }
    }
}
