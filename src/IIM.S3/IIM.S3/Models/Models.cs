

namespace IIM.S3.Models
{
    // File: Models.cs
    // Purpose: strongly-typed DTOs and domain models (kept lean).

    public sealed record StoragePaths(string BasePath, string CasPath, string TempPath, string DbPath);

    public sealed class BucketCreateRequest
    {
        public string Bucket { get; set; } = default!;
        public BucketConfiguration? Config { get; set; }
    }

    public sealed class PresignRequest
    {
        public string Bucket { get; set; } = default!;
        public string Key { get; set; } = default!;
        public string Operation { get; set; } = "GET";
        public int ExpirySeconds { get; set; } = 3600;
    }

    public sealed class CompletePart
    {
        public int PartNumber { get; set; }
        public string ETag { get; set; } = default!; // quoted md5 hex
    }

    public sealed class PresignedUrlRequest
    {
        public string Bucket { get; set; } = default!;
        public string Key { get; set; } = default!;
        public string Operation { get; set; } = "GET";
        public int ExpirySeconds { get; set; } = 3600;
        public string UserId { get; set; } = "system";
    }

    public sealed class ObjectMetadata
    {
        public string Bucket { get; set; } = default!;
        public string Key { get; set; } = default!;
        public string PhysicalPath { get; set; } = default!;
        public long Size { get; set; }
        public string ContentType { get; set; } = "application/octet-stream";
        public string MD5 { get; set; } = "";
        public string SHA256 { get; set; } = "";
        public string SHA512 { get; set; } = "";
        public bool IsDeduplicated { get; set; }
        public DateTimeOffset Created { get; set; }
        public DateTimeOffset? Deleted { get; set; }
        public string StorageClass { get; set; } = "STANDARD";
        public string VersionId { get; set; } = Guid.NewGuid().ToString("N");
        public ObjectLockInfo? ObjectLock { get; set; }
    }

    public sealed class ObjectLockInfo
    {
        public string Mode { get; set; } = "NONE"; // NONE/GOVERNANCE/COMPLIANCE
        public DateTimeOffset? RetainUntil { get; set; }
        public bool LegalHold { get; set; }

        // FIX: lock check considers governance bypass (handled by service)
        public bool IsLocked(DateTimeOffset now, bool allowGovernanceBypass) =>
            LegalHold || (RetainUntil is DateTimeOffset ru && now < ru && !allowGovernanceBypass && Mode == "GOVERNANCE")
            || (RetainUntil is DateTimeOffset ru2 && now < ru2 && Mode == "COMPLIANCE");
    }

    public sealed class BucketConfiguration
    {
        public bool EnableDeduplication { get; set; } = true;
        public bool ObjectLockEnabled { get; set; } = false;
        public int DefaultRetentionDays { get; set; } = 90;
        public string StorageClass { get; set; } = "STANDARD";
        public string SmbShareName { get; set; } = "";
    }

    public sealed class CustodyEntry
    {
        public string Action { get; set; } = "";
        public string User { get; set; } = "";
        public DateTimeOffset Timestamp { get; set; }
        public string Details { get; set; } = "";
    }

    public sealed class MultipartUpload
    {
        public string UploadId { get; set; } = default!;
        public string Bucket { get; set; } = default!;
        public string Key { get; set; } = default!;
        public DateTimeOffset Initiated { get; set; }
    }

    public sealed class UploadPart
    {
        public string UploadId { get; set; } = default!;
        public int PartNumber { get; set; }
        public long Size { get; set; }
        public string Md5Hex { get; set; } = "";
        public string Path { get; set; } = default!;
    }

    public sealed class ListingResult
    {
        public List<ListedObject> Objects { get; set; } = new();
        public List<string> CommonPrefixes { get; set; } = new();
    }

    public sealed class ListedObject
    {
        public string Key { get; set; } = default!;
        public long Size { get; set; }
        public DateTimeOffset LastModified { get; set; }
        public string ETag { get; set; } = default!;
        public string StorageClass { get; set; } = "STANDARD";
    }

    public sealed record HashBundle(string Md5Hex, string Sha256Hex, string Sha512Hex, long Bytes);

    public sealed record RoutingDecision(bool RequiresQuarantine, string OriginalBucket, string Reason);

}
