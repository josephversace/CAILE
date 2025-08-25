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
    public record SearchEvidenceRequest(
        string? SearchTerm,
        string? CaseId,
        EvidenceType? EvidenceType,
        DateTimeOffset? StartDate,
        DateTimeOffset? EndDate);

    /// <summary>
    /// Request to update evidence metadata
    /// </summary>
    public record UpdateEvidenceMetadataRequest(
        EvidenceMetadata Metadata);

    /// <summary>
    /// Request to add chain of custody entry
    /// </summary>
    public record AddChainOfCustodyRequest(
        string Action,
        string Details,
        string? Notes,
        Dictionary<string, object>? Metadata);

    /// <summary>
    /// Evidence integrity verification result
    /// </summary>
    public record IntegrityVerificationResult(
        bool IsValid,
        string ExpectedHash,
        string? ActualHash,
        string? ErrorMessage,
        DateTimeOffset VerifiedAt);
}
