using IIM.Shared.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;

namespace IIM.Infrastructure.Storage
{
   

    public class SeaweedFSStorageService : IS3StorageService
    {
        private readonly ILogger<SeaweedFSStorageService> _logger;
        private readonly S3StorageConfiguration _config;  // Keep the same config class
        private readonly IAmazonS3 _s3Client;  // Changed from IMinioClient
        private readonly IDeduplicationService _deduplicationService;

        // Standard bucket names - unchanged
        private const string MODELS_BUCKET = "iim-models";
        private const string EVIDENCE_BUCKET = "iim-evidence";
        private const string CASES_BUCKET = "iim-cases";
        private const string CHUNKS_BUCKET = "iim-chunks";

        public SeaweedFSStorageService(
            ILogger<SeaweedFSStorageService> logger,
            IOptions<S3StorageConfiguration> config,
            IDeduplicationService deduplicationService)
        {
            _logger = logger;
            _config = config.Value;
            _deduplicationService = deduplicationService;

            // Initialize AWS S3 client instead of MinIO client
            var s3Config = new AmazonS3Config
            {
                ServiceURL = $"http://{_config.Endpoint}",
                ForcePathStyle = true,  // Required for SeaweedFS
                UseHttp = !_config.UseSSL,
                SignatureVersion = "2"  // SeaweedFS works better with V2
            };

            _s3Client = new AmazonS3Client(
                _config.AccessKey,
                _config.SecretKey,
                s3Config);

            // Initialize buckets
            _ = InitializeBucketsAsync();
        }

        private async Task InitializeBucketsAsync()
        {
            var buckets = new[] { MODELS_BUCKET, EVIDENCE_BUCKET, CASES_BUCKET, CHUNKS_BUCKET };

            foreach (var bucket in buckets)
            {
                try
                {
                    await CreateBucketAsync(bucket);
                    _logger.LogInformation("Initialized bucket: {Bucket}", bucket);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize bucket: {Bucket}", bucket);
                }
            }
        }

        public async Task<bool> CreateBucketAsync(string bucketName, CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if bucket exists
                try
                {
                    await _s3Client.GetBucketLocationAsync(bucketName, cancellationToken);
                    _logger.LogInformation("Bucket {Bucket} already exists", bucketName);
                    return true;
                }
                catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // Bucket doesn't exist, create it
                }

                // Create bucket
                await _s3Client.PutBucketAsync(new PutBucketRequest
                {
                    BucketName = bucketName,
                    BucketRegion = S3Region.USEast1  // SeaweedFS doesn't care about region
                }, cancellationToken);

                _logger.LogInformation("Created bucket: {Bucket}", bucketName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create bucket: {Bucket}", bucketName);
                return false;
            }
        }

        public async Task<string> PutObjectAsync(
            string bucketName,
            string objectName,
            Stream data,
            Dictionary<string, string>? metadata = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Calculate hash first if deduplication is enabled
                string hash = null;
                if (_config.EnableDeduplication)
                {
                    data.Position = 0;
                    hash = await _deduplicationService.ComputeHashAsync(data, cancellationToken);
                    data.Position = 0;
                }

                var request = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = objectName,
                    InputStream = data,
                    ServerSideEncryptionMethod = ServerSideEncryptionMethod.None
                };

                // Add metadata if provided
                if (metadata != null)
                {
                    foreach (var kvp in metadata)
                    {
                        request.Metadata.Add(kvp.Key, kvp.Value);
                    }
                }

                // Add hash as metadata for deduplication
                if (!string.IsNullOrEmpty(hash))
                {
                    request.Metadata["x-amz-meta-sha256"] = hash;
                }

                var response = await _s3Client.PutObjectAsync(request, cancellationToken);

                _logger.LogInformation("Stored object {Object} in bucket {Bucket}, ETag: {ETag}",
                    objectName, bucketName, response.ETag);

                // Return hash or ETag
                return hash ?? response.ETag.Trim('"');
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store object: {Object}", objectName);
                throw;
            }
        }

        public async Task<Stream> GetObjectAsync(
            string bucketName,
            string objectName,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new GetObjectRequest
                {
                    BucketName = bucketName,
                    Key = objectName
                };

                var response = await _s3Client.GetObjectAsync(request, cancellationToken);

                // Copy to memory stream to ensure we can return a seekable stream
                var memoryStream = new MemoryStream();
                await response.ResponseStream.CopyToAsync(memoryStream, cancellationToken);
                memoryStream.Position = 0;

                _logger.LogInformation("Retrieved object {Object} from bucket {Bucket}",
                    objectName, bucketName);

                return memoryStream;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Object not found: {Object} in bucket {Bucket}",
                    objectName, bucketName);
                throw new FileNotFoundException($"Object {objectName} not found in bucket {bucketName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve object: {Object}", objectName);
                throw;
            }
        }

        public async Task<bool> DeleteObjectAsync(
            string bucketName,
            string objectName,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new DeleteObjectRequest
                {
                    BucketName = bucketName,
                    Key = objectName
                };

                await _s3Client.DeleteObjectAsync(request, cancellationToken);

                _logger.LogInformation("Deleted object: {Object} from bucket: {Bucket}",
                    objectName, bucketName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete object: {Object} from bucket: {Bucket}",
                    objectName, bucketName);
                return false;
            }
        }

        public async Task<bool> ObjectExistsAsync(
            string bucketName,
            string objectName,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new GetObjectMetadataRequest
                {
                    BucketName = bucketName,
                    Key = objectName
                };

                var response = await _s3Client.GetObjectMetadataAsync(request, cancellationToken);
                return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if object exists: {Object} in bucket: {Bucket}",
                    objectName, bucketName);
                throw;
            }
        }
    }
}