using System;
using System.Collections.Generic;
using System.Text;

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

		public static ExtractionResult Empty() => new()
		{
			Indicators = new List<Indicator>(),
			Occurrences = new List<IndicatorOccurrence>(),
			Statistics = new ExtractionStatistics()
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

}
