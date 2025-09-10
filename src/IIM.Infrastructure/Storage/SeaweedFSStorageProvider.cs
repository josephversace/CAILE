using Amazon.S3;
using Amazon.S3.Model;
using IIM.Shared.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace IIM.Infrastructure.Storage
{
    /// <summary>
    /// Implements the IObjectStorageProvider for a SeaweedFS instance using its S3 compatible API.
    /// This class is a lean adapter, responsible only for generating pre-signed URLs and deleting objects.
    /// It does not contain business logic like deduplication or bucket creation on startup.
    /// </summary>
    public class SeaweedFSStorageProvider : IObjectStorageProvider
    {
        private readonly ILogger<SeaweedFSStorageProvider> _logger;
        private readonly IAmazonS3 _s3Client;

        public SeaweedFSStorageProvider(
            ILogger<SeaweedFSStorageProvider> logger,
            IOptions<S3StorageConfiguration> configOptions)
        {
            _logger = logger;
            var config = configOptions.Value;

            // Configure the S3 client to connect to the SeaweedFS endpoint.
            var s3Config = new AmazonS3Config
            {
                ServiceURL = $"http://{config.Endpoint}",
                ForcePathStyle = true, // This is crucial for SeaweedFS and other S3 compatibles.
                UseHttp = !config.UseSSL,
                SignatureVersion = "4" // V4 is the modern standard and is supported.
            };

            _s3Client = new AmazonS3Client(config.AccessKey, config.SecretKey, s3Config);
            _logger.LogInformation("SeaweedFS Storage Provider initialized for endpoint {Endpoint}", config.Endpoint);
        }

        /// <summary>
        /// Generates a temporary, pre-signed URL that can be used to upload a file directly to storage.
        /// </summary>
        public Task<string> GetPresignedUploadUrlAsync(string bucketName, string objectKey, TimeSpan expiry)
        {
            try
            {
                var request = new GetPreSignedUrlRequest
                {
                    BucketName = bucketName,
                    Key = objectKey,
                    Verb = HttpVerb.PUT,
                    Expires = DateTime.UtcNow.Add(expiry)
                };

                var url = _s3Client.GetPreSignedURL(request);
                _logger.LogDebug("Generated pre-signed upload URL for {Bucket}/{Key}", bucketName, objectKey);
                return Task.FromResult(url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate pre-signed upload URL for {Bucket}/{Key}", bucketName, objectKey);
                throw;
            }
        }

        /// <summary>
        /// Generates a temporary, pre-signed URL that can be used to download a file directly from storage.
        /// </summary>
        public Task<string> GetPresignedDownloadUrlAsync(string bucketName, string objectKey, TimeSpan expiry)
        {
            try
            {
                var request = new GetPreSignedUrlRequest
                {
                    BucketName = bucketName,
                    Key = objectKey,
                    Verb = HttpVerb.GET,
                    Expires = DateTime.UtcNow.Add(expiry)
                };

                var url = _s3Client.GetPreSignedURL(request);
                _logger.LogDebug("Generated pre-signed download URL for {Bucket}/{Key}", bucketName, objectKey);
                return Task.FromResult(url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate pre-signed download URL for {Bucket}/{Key}", bucketName, objectKey);
                throw;
            }
        }

        /// <summary>
        /// Performs an efficient, server-side copy of an object from one location to another.
        /// </summary>
        public async Task CopyObjectAsync(string sourceBucket, string sourceKey, string destBucket, string destKey)
        {
            try
            {
                var request = new CopyObjectRequest
                {
                    SourceBucket = sourceBucket,
                    SourceKey = sourceKey,
                    DestinationBucket = destBucket,
                    DestinationKey = destKey
                };
                await _s3Client.CopyObjectAsync(request);
                _logger.LogInformation("Copied object from {SourceBucket}/{SourceKey} to {DestBucket}/{DestKey}",
                    sourceBucket, sourceKey, destBucket, destKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to copy object from {SourceBucket}/{SourceKey} to {DestBucket}/{DestKey}",
                    sourceBucket, sourceKey, destBucket, destKey);
                throw;
            }
        }

        /// <summary>
        /// Deletes an object from the specified bucket.
        /// </summary>
        public async Task DeleteObjectAsync(string bucketName, string objectKey)
        {
            try
            {
                var request = new DeleteObjectRequest
                {
                    BucketName = bucketName,
                    Key = objectKey
                };

                await _s3Client.DeleteObjectAsync(request);
                _logger.LogInformation("Deleted object {Object} from bucket {Bucket}", objectKey, bucketName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete object: {Object} from bucket: {Bucket}", objectKey, bucketName);
                throw;
            }
        }
    }
}

