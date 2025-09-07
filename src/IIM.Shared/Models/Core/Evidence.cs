using IIM.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Shared.Models
{

    /// <summary>
    /// Request to search evidence
    /// </summary>
    public record SearchFileRequest(
        string? SearchTerm,
        string? WorkspaceId,
        FileType? FileType,
        DateTimeOffset? StartDate,
        DateTimeOffset? EndDate);

    /// <summary>
    /// Request to update file metadata
    /// </summary>
    public record UpdateFileMetadataRequest(
        FileMetadata Metadata);


    /// <summary>
    /// Upload confirmation response
    /// Purpose: Confirm successful upload and integrity
    /// Used by: Upload UI to show completion status
    /// </summary>
    public class ConfirmFileUploadResponse
    {
        public bool Success { get; set; }
        public FileProcessingStatus Status { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ServerHash { get; set; }
        public bool HashesMatch { get; set; }
        public ChainOfCustodyEntry? InitialChainEntry { get; set; }
        public string? StoragePath { get; set; }
    }

    /// <summary>
    /// Request to add chain of custody entry
    /// </summary>
    public record AddChainOfCustodyRequest(
        string Action,
        string Details,
        string? Notes,
        Dictionary<string, object>? Metadata);

    /// <summary>
    /// File integrity verification result
    /// </summary>
    public record IntegrityVerificationResult(
        bool IsValid,
        string ExpectedHash,
        string? ActualHash,
        string? ErrorMessage,
        DateTimeOffset VerifiedAt);
}
