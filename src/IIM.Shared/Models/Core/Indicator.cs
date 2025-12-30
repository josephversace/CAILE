using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IIM.Shared.Dtos;

namespace IIM.Shared.Models
{
	public sealed class Indicator
	{
		public Guid Id { get; set; }
		public IndicatorType Type { get; set; }
		public string? Subtype { get; set; }
		public required string Value { get; set; }
		public required string NormalizedValue { get; set; }
		public string? RawValue { get; set; } // Original defanged form
		public List<Guid> Occurrences { get; set; } = new();
		public List<string>? RelatedValues { get; set; }
		public int FirstSeen { get; set; }
		public float Confidence { get; set; } = 1.0f;
		public Dictionary<string, string>? Metadata { get; set; }
	}

	public sealed class EntityGroup
	{
		public Guid GroupId { get; set; } = Guid.NewGuid();

		public EntityCategory Category { get; set; }
		public string Label { get; set; } // e.g., "User Profile", "Contact Info"
		public List<IndicatorOccurrence> Members { get; set; } = new();
		public float GroupConfidence => Members.Any() ? Members.Average(m => m.Confidence) : 0f;
	}

	public sealed class IndicatorOccurrence
	{
		public Guid Id { get; set; }
		public Guid? IndicatorId { get; set; }
		public Guid? DerivedFromId { get; set; }
		public IndicatorType Type { get; set; }
		public string? Subtype { get; set; }
		public required string Value { get; set; }
		public string? RawValue { get; set; } // Original defanged form if applicable
		public int Offset { get; set; }
		public int Length { get; set; }
		public float Confidence { get; set; } = 1.0f;
		public required IndicatorContext Context { get; set; }
		public Dictionary<string, string>? Metadata { get; set; }
	}

	public sealed class IndicatorContext
	{
		public required string Sentence { get; set; }
		public required string Block { get; set; }
		public required string Surrounding { get; set; }
		public required string SurroundingLower { get; set; } // Cached lowercase
		public List<string> PrecedingWords { get; set; } = new();
		public List<string> FollowingWords { get; set; } = new();
	}


	public sealed class ExtractionResult
	{
		public required List<Indicator> Indicators { get; set; }
		public required List<IndicatorOccurrence> Occurrences { get; set; }
		public ExtractionStatistics Statistics { get; set; } = new();

		public List<EntityGroupDto> IdentityGroups { get; set; }

		public List<ProposedEventDto> ProposedEvents { get; set; } = new();
		public static ExtractionResult Empty() => new()
		{
			Indicators = new List<Indicator>(),
			Occurrences = new List<IndicatorOccurrence>(),
			Statistics = new ExtractionStatistics(),
			IdentityGroups = new List<EntityGroupDto>(),
			ProposedEvents = new List<ProposedEventDto>(),
		};
	}


	public sealed class ExtractionStatistics
	{
		public int OriginalTextLength { get; set; }
		public int TotalOccurrencesBeforeFiltering { get; set; }
		public int TotalOccurrences { get; set; }
		public int UniqueIndicators { get; set; }
		public bool TimedOut { get; set; }
		public TimeSpan ExtractionDuration { get; set; }
		public Dictionary<IndicatorType, int> OccurrencesByType { get; set; } = new();
		public Dictionary<IndicatorType, ConfidenceStats> ConfidenceDistribution { get; set; } = new();
	}

	public sealed class ConfidenceStats
	{
		public float Min { get; set; }
		public float Max { get; set; }
		public float Average { get; set; }
	}


	/// Content model for IndicatorCollection workspace artifacts.
	/// Serialized to JSON and stored in WorkspaceArtifact.Content.
	/// </summary>
	public class IndicatorCollectionContent
	{
		public IndicatorType IndicatorType { get; set; }
		public List<CollectedIndicator> Indicators { get; set; } = new();
	}

	/// <summary>
	/// A single indicator collected into a workspace, with provenance tracking.
	/// </summary>
	public class CollectedIndicator
	{
		public Guid Id { get; set; } = Guid.NewGuid();
		public string Value { get; set; } = "";
		public string? Subtype { get; set; }  // IPv4, SHA256, Bitcoin, etc.
		public float HighestConfidence { get; set; }
		public DateTimeOffset FirstSeen { get; set; }
		public DateTimeOffset LastSeen { get; set; }
		public List<IndicatorSource> Sources { get; set; } = new();
	}

	/// <summary>
	/// Tracks where an indicator was found - for chain of custody / auditability.
	/// </summary>
	public class IndicatorSource
	{
		public Guid FileId { get; set; }
		public string FileName { get; set; } = "";
		public Guid? ExtractionArtifactId { get; set; }
		public DateTimeOffset AddedUtc { get; set; }
	}
}