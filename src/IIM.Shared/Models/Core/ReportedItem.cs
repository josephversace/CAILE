using System;
using System.Collections.Generic;
using System.Text;
using static Google.Protobuf.Reflection.ExtensionRangeOptions.Types;

namespace IIM.Shared.Models
{
	public sealed class ReportedItem
	{
		public Guid Id { get; init; } = Guid.NewGuid();

		// High-level classification
		public ReportedItemKind Kind { get; init; }

		// Optional subtype (used heavily for identifiers)
		public IdentifierType? IdentifierType { get; init; }

		// The literal value as reported
		public string Value { get; init; } = string.Empty;

		// Human-readable description of how it appears in the document
		public string? Description { get; init; }

		// Provenance
		public Guid SourceFileId { get; init; }
		public string SourceDocumentHash { get; init; } = string.Empty;

		public string ReportedBy { get; init; } = string.Empty; // e.g. "TikTok", "Google", "User Upload"
		public VerificationStatus Verification { get; init; } = VerificationStatus.Unknown;

		// Confidence as reported or inferred by extraction (NOT truth)
		public double Confidence { get; init; } = 0.6;

		// GraphRAG traceability
		public string? ExtractionEngine { get; init; } = "GraphRAG";
		public string? ExtractionVersion { get; init; }
		public IReadOnlyList<string>? TextUnitIds { get; init; }

		// Audit
		public DateTimeOffset ReportedAt { get; init; } = DateTimeOffset.UtcNow;

		// Promotion tracking
		public Guid? PromotedEntityArtifactId { get; private set; }

		public bool IsPromoted => PromotedEntityArtifactId.HasValue;

		public void MarkPromoted(Guid entityArtifactId)
		{
			PromotedEntityArtifactId = entityArtifactId;
		}
	}

	public sealed class ReportedAssociation
	{
		public Guid Id { get; init; } = Guid.NewGuid();

		public Guid SourceItemId { get; init; }
		public Guid TargetItemId { get; init; }

		public string AssociationType { get; init; } = "reported_association";

		public string? Description { get; init; }

		public double Weight { get; init; } = 0.5;

		public string Source { get; init; } = "GraphRAG";

		public Guid SourceFileId { get; init; }

		public DateTimeOffset ReportedAt { get; init; } = DateTimeOffset.UtcNow;
	}


	public enum ReportedItemKind
	{
		Identifier,     // Email, phone, IP, crypto address, username
		Account,        // Platform account, service account
		Name,           // Display name, alias, legal name (reported)
		Organization,   // Company, agency, platform
		Location,       // Country, city, address
		Event,          // Upload, transaction, login, report
		File,           // Uploaded file, hash-referenced content
		Technology,     // Platform, app, protocol
		Other
	}

	public enum IdentifierType
	{
		Email,
		Phone,
		Username,
		IpAddress,
		CryptoAddress,
		Domain,
		Url,
		Hash,
		Other
	}

	public enum VerificationStatus
	{
		Unknown,
		Unverified,
		SelfReported,
		PlatformReported,
		ThirdPartyReported,
		Verified
	}

}
