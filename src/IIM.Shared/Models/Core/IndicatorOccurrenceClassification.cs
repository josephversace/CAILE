using System;
using System.Collections.Generic;
using System.Text;

namespace IIM.Shared.Models
{
	public sealed record IndicatorOccurrenceClassification
	   (
		   Guid OccurrenceId,
		   string Role,
		   double Confidence,
		   string? Rationale
	   );
}
