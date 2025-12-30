using System;
using System.Collections.Generic;
using System.Text;

namespace IIM.Shared.Dtos
{
	public class ProposedEventDto
	{
		public Guid Id { get; set; }
		public string EventType { get; set; }
		public string Timestamp { get; set; }
		public string SharedContext { get; set; } // The context string for the anchor

		// Just the basics: Type and Value
		public List<IndicatorSummary> Who { get; set; } = new();
		public List<IndicatorSummary> What { get; set; } = new();
		public List<IndicatorSummary> Where { get; set; } = new();
	}

	public record IndicatorSummary(string Type, string Value);
}
