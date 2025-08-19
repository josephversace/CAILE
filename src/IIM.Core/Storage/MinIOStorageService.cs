using IIM.Core.Configuration;
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

namespace IIM.Core.Storage
{
    public class MinIOStorageService : IMinIOStorageService
    {
        private readonly ILogger<MinIOStorageService> _logger;
        private readonly MinIOConfiguration _config;
        private readonly IMinioClient _minioClient;
        private readonly IDeduplicationService _deduplicationService;

        // Standard bucket names
        private const string MODELS_BUCKET = "iim-models";
        private const string EVIDENCE_BUCKET = "iim-evidence";
        private const string CASES_BUCKET = "iim-cases";
        private const string CHUNKS_BUCKET = "iim-chunks";

        public MinIOStorageService(
            ILogger<MinIOStorageService> logger,
            IOptions<MinIOConfiguration> config,
            IDeduplicationService deduplicationService)
        {
            _logger = logger;
            _config = config.Value;
            _deduplicationService = deduplicationService;

            // Initialize MinIO client
            _minioClient = new MinioClient()
                .WithEndpoint(_config.Endpoint)
                .WithCredentials(_config.AccessKey, _config.SecretKey)
                .WithSSL(_config.UseSSL)
                .Build();

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
                var exists = await _minioClient.BucketExistsAsync(
                    new BucketExistsArgs().WithBucket(bucketName),
                    cancellationToken);

                if (!exists)
                {
                    await _minioClient.MakeBucketAsync(
                        new MakeBucketArgs()
                            .WithBucket(bucketName)
                            .WithLocation(_config.Region),
                        cancellationToken);

                    _logger.LogInformation("Created bucket: {Bucket}", bucketName);
                }
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
                // Store directly for now (we'll add deduplication later)
                await _minioClient.PutObjectAsync(new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithStreamData(data)
                    .WithObjectSize(data.Length)
                    .WithHeaders(metadata),
                    cancellationToken);

                // Compute and return hash
                data.Position = 0;
                return await _deduplicationService.ComputeHashAsync(data, cancellationToken);
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
                var memoryStream = new MemoryStream();
                await _minioClient.GetObjectAsync(new GetObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithCallbackStream(stream => stream.CopyTo(memoryStream)),
                    cancellationToken);

                memoryStream.Position = 0;
                return memoryStream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve object: {Object}", objectName);
                throw;
            }
        }

        public async Task<bool> DeleteObjectAsync(string bucketName, string objectName, CancellationToken cancellationToken = default)
        {
            try
            {
                await _minioClient.RemoveObjectAsync(new RemoveObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName),
                    cancellationToken);

                _logger.LogInformation("Deleted object: {Object} from bucket: {Bucket}", objectName, bucketName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete object: {Object} from bucket: {Bucket}", objectName, bucketName);
                return false;
            }
        }

        public async Task<bool> ObjectExistsAsync(string bucketName, string objectName, CancellationToken cancellationToken = default)
        {
            try
            {
                await _minioClient.StatObjectAsync(new StatObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName),
                    cancellationToken);
                return true;
            }
            catch (ObjectNotFoundException)
            {
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if object exists: {Object} in bucket: {Bucket}", objectName, bucketName);
                return false;
            }
        }
    }
}