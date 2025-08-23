using IIM.Shared.Enums;
using IIM.Shared.DTOs;
using System;
using System.Collections.Generic;

namespace IIM.Shared.DTOs
{
    /// <summary>
    /// Request to initiate evidence upload with pre-computed hash
    /// </summary>
    public class InitiateEvidenceUploadRequest
    {
        /// <summary>
        /// SHA-256 hash of the file computed by client
        /// </summary>
        public string FileHash { get; set; } = string.Empty;

        /// <summary>
        /// Original filename
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// File size in bytes
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// MIME type of the file
        /// </summary>
        public string ContentType { get; set; } = "application/octet-stream";

        /// <summary>
        /// Evidence metadata including case number, collector, etc.
        /// </summary>
        public EvidenceMetadata Metadata { get; set; } = new();
    }

    /// <summary>
    /// Response after checking for duplicates and generating upload URL
    /// </summary>
    public class InitiateEvidenceUploadResponse
    {
        /// <summary>
        /// Unique evidence ID for this upload
        /// </summary>
        public string EvidenceId { get; set; } = string.Empty;

        /// <summary>
        /// Status of the upload request
        /// </summary>
        public EvidenceUploadStatus Status { get; set; }

        /// <summary>
        /// Pre-signed URL for direct upload to MinIO (null if duplicate)
        /// </summary>
        public string? UploadUrl { get; set; }

        /// <summary>
        /// Expiration time for the upload URL
        /// </summary>
        public DateTimeOffset? UploadUrlExpires { get; set; }

        /// <summary>
        /// If duplicate, reference to existing evidence
        /// </summary>
        public string? DuplicateEvidenceId { get; set; }

        /// <summary>
        /// If duplicate, details about original upload
        /// </summary>
        public DuplicateInfo? DuplicateInfo { get; set; }

        /// <summary>
        /// Additional headers required for upload (if any)
        /// </summary>
        public Dictionary<string, string>? RequiredHeaders { get; set; }
    }

    /// <summary>
    /// Information about duplicate evidence
    /// </summary>
    public class DuplicateInfo
    {
        /// <summary>
        /// Original evidence ID
        /// </summary>
        public string OriginalEvidenceId { get; set; } = string.Empty;

        /// <summary>
        /// When the original was uploaded
        /// </summary>
        public DateTimeOffset OriginalUploadDate { get; set; }

        /// <summary>
        /// Who uploaded the original
        /// </summary>
        public string OriginalUploadedBy { get; set; } = string.Empty;

        /// <summary>
        /// Case number of original upload
        /// </summary>
        public string OriginalCaseNumber { get; set; } = string.Empty;

        /// <summary>
        /// How many times this file has been seen
        /// </summary>
        public int DuplicateCount { get; set; }
    }

    /// <summary>
    /// Request to confirm upload completion
    /// </summary>
    public class ConfirmEvidenceUploadRequest
    {
        /// <summary>
        /// Evidence ID from initiate response
        /// </summary>
        public string EvidenceId { get; set; } = string.Empty;

        /// <summary>
        /// ETag returned by MinIO after upload
        /// </summary>
        public string? ETag { get; set; }

        /// <summary>
        /// Client can re-confirm the hash
        /// </summary>
        public string? ClientHash { get; set; }
    }

    /// <summary>
    /// Response after confirming upload
    /// </summary>
    public class ConfirmEvidenceUploadResponse
    {
        /// <summary>
        /// Whether the upload was successfully verified
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Final status of the evidence
        /// </summary>
        public EvidenceStatus Status { get; set; }

        /// <summary>
        /// Error message if verification failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Server-computed hash for verification
        /// </summary>
        public string? ServerHash { get; set; }

        /// <summary>
        /// Whether hashes matched (if verification was performed)
        /// </summary>
        public bool? HashesMatch { get; set; }
    }

    /// <summary>
    /// Status of evidence upload
    /// </summary>
    public enum EvidenceUploadStatus
    {
        /// <summary>
        /// New upload initiated
        /// </summary>
        Initiated,

        /// <summary>
        /// File is duplicate of existing evidence
        /// </summary>
        Duplicate,

        /// <summary>
        /// Error during initiation
        /// </summary>
        Error
    }
}
