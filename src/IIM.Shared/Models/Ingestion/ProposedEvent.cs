using System;
using System.Collections.Generic;
using System.Text;

namespace IIM.Shared.Models
{
	public class ProposedEvent
	{
		public Guid Id { get; set; }
		public string EventType { get; set; }  // Upload, Login, Sent, etc.
		public IndicatorOccurrence Timestamp { get; set; }
		public List<IndicatorOccurrence> Who { get; set; }
		public List<IndicatorOccurrence> What { get; set; }
		public List<IndicatorOccurrence> Where { get; set; }

		public IndicatorContext Context { get; set; }
		public float Confidence { get; set; }
	}
}
