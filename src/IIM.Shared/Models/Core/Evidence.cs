using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using IIM.Shared.Enums;

namespace IIM.Shared.Models
{
    /// <summary>
    /// Domain model representing digital evidence
    /// </summary>
    public class Evidence
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string CaseId { get; set; } = string.Empty;
        public string CaseNumber { get; set; } = string.Empty;

        // File Information
        public string OriginalFileName { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty; // MIME type
        public long FileSize { get; set; }
        public string StoragePath { get; set; } = string.Empty;

        // Evidence Properties
        public EvidenceType Type { get; set; }
        public EvidenceStatus Status { get; set; } = EvidenceStatus.Pending;

        // Integrity
        public string Hash { get; set; } = string.Empty;
        public string HashAlgorithm { get; set; } = "SHA256";
        public bool IntegrityVerified { get; set; }
        public string DigitalSignature { get; set; } = string.Empty;

        // Metadata
        public string CollectedBy { get; set; } = string.Empty;
        public DateTimeOffset CollectionDate { get; set; } = DateTimeOffset.UtcNow;
        public string? CollectionLocation { get; set; }
        public string? DeviceSource { get; set; }
        public string? Description { get; set; }

        // Timestamps
        public DateTimeOffset IngestTimestamp { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? ProcessedAt { get; set; }
        public DateTimeOffset? ArchivedAt { get; set; }

        // Chain of Custody
        public List<ChainOfCustodyEntry> ChainOfCustody { get; set; } = new();

        // Processing Results
        public List<ProcessedEvidence> ProcessedVersions { get; set; } = new();
        public List<AnalysisResult> AnalysisResults { get; set; } = new();

        // Additional Data
        public Dictionary<string, string> CustomFields { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();

        // Business Methods

        /// <summary>
        /// Calculates and sets the hash for the evidence
        /// </summary>
        public void CalculateHash(byte[] data)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(data);
                Hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }

        /// <summary>
        /// Verifies the integrity of the evidence
        /// </summary>
        public bool VerifyIntegrity(byte[] data)
        {
            var originalHash = Hash;
            CalculateHash(data);
            var isValid = Hash.Equals(originalHash, StringComparison.OrdinalIgnoreCase);
            IntegrityVerified = isValid;
            return isValid;
        }

        /// <summary>
        /// Adds a chain of custody entry
        /// </summary>
        public void AddChainOfCustodyEntry(string action, string actor, string details)
        {
            var previousHash = ChainOfCustody.Count > 0
                ? ChainOfCustody[^1].Hash
                : Hash;

            var entry = new ChainOfCustodyEntry
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = DateTimeOffset.UtcNow,
                Action = action,
                Actor = actor,
                Details = details,
                PreviousHash = previousHash
            };

            // Calculate entry hash
            var entryData = $"{entry.Timestamp}{entry.Action}{entry.Actor}{entry.Details}{entry.PreviousHash}";
            using (var sha = SHA256.Create())
            {
                var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(entryData));
                entry.Hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }

            ChainOfCustody.Add(entry);
        }

        /// <summary>
        /// Marks evidence as processed
        /// </summary>
        public void MarkAsProcessed(ProcessedEvidence processedVersion)
        {
            ProcessedVersions.Add(processedVersion);
            Status = EvidenceStatus.Processed;
            ProcessedAt = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Archives the evidence
        /// </summary>
        public void Archive(string archivedBy)
        {
            if (Status == EvidenceStatus.Pending)
                throw new InvalidOperationException("Cannot archive pending evidence");

            Status = EvidenceStatus.Archived;
            ArchivedAt = DateTimeOffset.UtcNow;
            AddChainOfCustodyEntry("Archived", archivedBy, "Evidence archived for long-term storage");
        }

        /// <summary>
        /// Determines if evidence can be deleted
        /// </summary>
        public bool CanDelete()
        {
            // Evidence cannot be deleted if it's archived or part of a closed case
            return Status != EvidenceStatus.Archived;
        }

        /// <summary>
        /// Validates the chain of custody
        /// </summary>
        public bool ValidateChainOfCustody()
        {
            if (ChainOfCustody.Count == 0)
                return true;

            var previousHash = Hash;
            foreach (var entry in ChainOfCustody)
            {
                if (entry.PreviousHash != previousHash)
                    return false;

                // Verify entry hash
                var entryData = $"{entry.Timestamp}{entry.Action}{entry.Actor}{entry.Details}{entry.PreviousHash}";
                using (var sha = SHA256.Create())
                {
                    var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(entryData));
                    var calculatedHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

                    if (calculatedHash != entry.Hash)
                        return false;
                }

                previousHash = entry.Hash;
            }

            return true;
        }

        /// <summary>
        /// Gets the evidence age
        /// </summary>
        public TimeSpan GetAge()
        {
            return DateTimeOffset.UtcNow - IngestTimestamp;
        }
    }
}