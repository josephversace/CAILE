using System;
using System.Collections.Generic;
using System.Text;

namespace IIM.Shared.Dtos
{
	public record ProposedEventDto
	{
		public Guid Id { get; init; }
		public string EventType { get; init; }
		public string Timestamp { get; init; }

		// Instead of string SharedContext, store where the context is
		public int ContextStart { get; init; }
		public int ContextLength { get; init; }

		public List<IndicatorSummary> Who { get; init; }
		public List<IndicatorSummary> What { get; init; }
		public List<IndicatorSummary> Where { get; init; }
	}
	public record IndicatorSummary(string Type, string Value);
}
