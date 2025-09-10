using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces;

/// <summary>
/// Provides a generic, technology-agnostic interface for interacting with an object storage backend.
/// </summary>
public interface IObjectStorageProvider
{
    // ==========================================================================================
    // Pre-signed URL Methods (for client-side operations)
    // ==========================================================================================

    /// <summary>
    /// Generates a temporary, pre-signed URL that allows a client to upload a file directly to storage.
    /// </summary>
    Task<string> GetPresignedUploadUrlAsync(string bucketName, string objectKey, TimeSpan expiry);

    /// <summary>
    /// Generates a temporary, pre-signed URL that allows a client to download a file directly from storage.
    /// </summary>
    Task<string> GetPresignedDownloadUrlAsync(string bucketName, string objectKey, TimeSpan expiry);

    // ==========================================================================================
    // Direct Stream Methods (for internal, server-side operations)
    // ==========================================================================================

    /// <summary>
    /// Directly uploads an object's content from a stream. Use this for server-to-server operations.
    /// </summary>
    Task PutObjectAsync(string bucketName, string objectKey, Stream data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an object's content as a stream. Use this for server-to-server operations.
    /// </summary>
    Task<Stream> GetObjectAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default);

    // ==========================================================================================
    // Management Methods
    // ==========================================================================================

    /// <summary>
    /// Performs an efficient, server-side copy of an object from one location to another.
    /// </summary>
    Task CopyObjectAsync(string sourceBucket, string sourceKey, string destBucket, string destKey);

    /// <summary>
    /// Deletes an object from storage.
    /// </summary>
    Task DeleteObjectAsync(string bucketName, string objectKey);
}

