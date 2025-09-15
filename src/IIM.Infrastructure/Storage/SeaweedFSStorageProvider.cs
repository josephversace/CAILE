using Amazon.S3;
using Amazon.S3.Model;
using IIM.Shared.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Infrastructure.Storage
{
    public class SeaweedFSStorageProvider : IObjectStorageProvider
    {
        private readonly ILogger<SeaweedFSStorageProvider> _logger;
        private readonly IAmazonS3 _s3Client;

        public SeaweedFSStorageProvider(
            ILogger<SeaweedFSStorageProvider> logger,
            IOptions<S3StorageConfiguration> config)
        {
            _logger = logger;
            var s3Config = config.Value;

            var amazonS3Config = new AmazonS3Config
            {
                ServiceURL = $"http://{s3Config.Endpoint}",
                ForcePathStyle = true,
                UseHttp = !s3Config.UseSSL,
                SignatureVersion = "4"
            };

            _s3Client = new AmazonS3Client(
                s3Config.AccessKey,
                s3Config.SecretKey,
                amazonS3Config);
        }

        public Task<string> GetPresignedUploadUrlAsync(string bucketName, string objectKey, TimeSpan expiry)
        {
            ValidateBucketAndKey(bucketName, objectKey);

            var request = new GetPreSignedUrlRequest
            {
                BucketName = bucketName,
                Key = objectKey,
                Verb = HttpVerb.PUT,
                Expires = DateTime.UtcNow.Add(expiry)
            };
            var url = _s3Client.GetPreSignedURL(request);
            return Task.FromResult(url);
        }

        public Task<string> GetPresignedDownloadUrlAsync(string bucketName, string objectKey, TimeSpan expiry)
        {
            ValidateBucketAndKey(bucketName, objectKey);

            var request = new GetPreSignedUrlRequest
            {
                BucketName = bucketName,
                Key = objectKey,
                Verb = HttpVerb.GET,
                Expires = DateTime.UtcNow.Add(expiry)
            };
            var url = _s3Client.GetPreSignedURL(request);
            return Task.FromResult(url);
        }

        public async Task PutObjectAsync(string bucketName, string objectKey, Stream data, CancellationToken cancellationToken = default)
        {
            ValidateBucketAndKey(bucketName, objectKey);

            try
            {
                var request = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = objectKey,
                    InputStream = data,
                };
                await _s3Client.PutObjectAsync(request, cancellationToken);
                _logger.LogInformation("Successfully stored object {ObjectKey} in bucket {BucketName}", objectKey, bucketName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store object {ObjectKey} in bucket {BucketName}", objectKey, bucketName);
                throw;
            }
        }

        public async Task<Stream> GetObjectAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default)
        {
            ValidateBucketAndKey(bucketName, objectKey);

            try
            {
                var request = new GetObjectRequest
                {
                    BucketName = bucketName,
                    Key = objectKey
                };
                var response = await _s3Client.GetObjectAsync(request, cancellationToken);
                return response.ResponseStream;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Object not found: {ObjectKey} in bucket {BucketName}", objectKey, bucketName);
                throw new FileNotFoundException($"Object {objectKey} not found in bucket {bucketName}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve object {ObjectKey} from bucket {BucketName}", objectKey, bucketName);
                throw;
            }
        }

        public async Task CopyObjectAsync(string sourceBucket, string sourceKey, string destBucket, string destKey)
        {
            ValidateBucketAndKey(sourceBucket, sourceKey);
            ValidateBucketAndKey(destBucket, destKey);

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
                _logger.LogInformation("Successfully copied {SourceKey} from {SourceBucket} to {DestKey} in {DestBucket}",
                    sourceKey, sourceBucket, destKey, destBucket);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to copy object from {SourceKey} to {DestKey}", sourceKey, destKey);
                throw;
            }
        }

        public async Task DeleteObjectAsync(string bucketName, string objectKey)
        {
            ValidateBucketAndKey(bucketName, objectKey);

            try
            {
                var request = new DeleteObjectRequest
                {
                    BucketName = bucketName,
                    Key = objectKey
                };
                await _s3Client.DeleteObjectAsync(request);
                _logger.LogInformation("Successfully deleted object {ObjectKey} from bucket {BucketName}", objectKey, bucketName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete object {ObjectKey} from bucket {BucketName}", objectKey, bucketName);
                throw;
            }
        }

        private static void ValidateBucketAndKey(string bucketName, string objectKey)
        {
            if (string.IsNullOrWhiteSpace(bucketName))
                throw new ArgumentException("Bucket name cannot be null or empty.", nameof(bucketName));
            if (string.IsNullOrWhiteSpace(objectKey))
                throw new ArgumentException("Object key cannot be null or empty.", nameof(objectKey));
        }
    }
}

