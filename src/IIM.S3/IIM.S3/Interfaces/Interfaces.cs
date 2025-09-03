using IIM.S3.Models;

namespace IIM.S3.Interfaces
{
    // File: Interfaces.cs
    // Purpose: clear contracts for the service layers.


    public interface IClock { DateTimeOffset UtcNow { get; } }

    public interface IPresignedUrlService
    {
        Task<string> GenerateAsync(PresignedUrlRequest request);
        Task<bool> ValidateAsync(string absoluteUrl, string operation);
    }

    public interface ISmbShareService
    {
        void InitializeShares();
        Task CreateShareAsync(string bucket, string? shareName);
        Task CreateSymlinkAsync(string bucket, string key, string physicalPath);
        Task RemoveSymlinkAsync(string bucket, string key);
    }

    public interface IHashingService
    {
        // Hash and write while streaming to disk (non-seekable safe).
        Task<HashBundle> HashAndWriteAsync(Stream src, string tempPath, CancellationToken ct = default);
        Task<(string md5Hex, long size)> HashMd5AndWriteAsync(Stream src, string tempPath, CancellationToken ct = default);
    }

    public interface IPolicyEngine
    {
        Task<bool> CanGeneratePresignedUrlAsync(string user, string bucket, string key, string operation);
        Task<bool> CanBypassGovernanceAsync(string user, string bucket, string key);
        Task<RoutingDecision> DetermineRoutingAsync(string bucket, string key, IDictionary<string, string> metadata);
    }

    public interface IStorageBackend
    {
        Task<string> CreateBucketDirectoryAsync(string bucket);
        Task<(string partPath, string md5Hex, long size)> StorePartAsync(string uploadId, int partNumber, Stream src, CancellationToken ct = default);
        Task<string> CombinePartsAsync(string uploadId, IEnumerable<UploadPart> parts, string tempOutPath, CancellationToken ct = default);
        Task CleanupPartsAsync(string uploadId);
        Task<string> MoveToFinalLocationAsync(string bucket, string key, string tempPath);
        Task<Stream> OpenReadAsync(string physicalPath);
    }

    public interface IDeduplicationService
    {
        Task<string?> GetPathByHashAsync(string sha256Hex);
        Task<string> PutCasAsync(string tempPath, string sha256Hex);
    }

    public interface IMetadataStore
    {
        void Init();

        // Buckets
        Task CreateBucketAsync(string bucket, BucketConfiguration cfg);
        Task<BucketConfiguration> GetBucketConfigAsync(string bucket);
        Task<List<string>> ListBucketsAsync();

        // Objects
        Task<ObjectMetadata?> GetLatestAsync(string bucket, string key);
        Task UpsertLatestAsync(ObjectMetadata meta);
        Task SoftDeleteLatestAsync(string bucket, string key, DateTimeOffset when);

        // Custody
        Task AppendCustodyEntryAsync(string bucket, string key, string versionId, CustodyEntry entry);

        // Multipart
        Task CreateMultipartAsync(MultipartUpload up);
        Task<MultipartUpload?> GetMultipartAsync(string uploadId);
        Task UpsertPartAsync(UploadPart part);
        Task<List<UploadPart>> ListPartsAsync(string uploadId);
        Task DeleteMultipartAsync(string uploadId);

        // Listing
        Task<IEnumerable<ObjectMetadata>> ListObjectsAsync(string bucket, string prefix);
    }

    public interface IEventBus
    {
        Task PublishAsync(string topic, object payload);
    }

    public interface IS3Service
    {
        Task CreateBucketAsync(string bucket, BucketConfiguration config);
        Task<ObjectMetadata> PutObjectAsync(string bucket, string key, HttpRequest request);
        Task GetObjectAsync(string bucket, string key, HttpRequest request, HttpResponse response);
        Task HeadObjectAsync(string bucket, string key, HttpResponse response);
        Task<bool> DeleteObjectAsync(string bucket, string key, HttpRequest request);
        Task<string> GeneratePresignedUrlAsync(string bucket, string key, string operation, int expirySeconds);

        Task<string> InitiateMultipartUploadAsync(string bucket, string key);
        Task<string> UploadPartAsync(string uploadId, int partNumber, HttpRequest request); // returns quoted md5 etag
        Task<(ObjectMetadata meta, string multipartEtag)> CompleteMultipartUploadAsync(string uploadId, IEnumerable<(int PartNumber, string ETag)> parts);

        Task<ListingResult> ListObjectsAsync(string bucket, string prefix, string? delimiter);
    }

}
