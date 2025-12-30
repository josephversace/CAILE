using System;
using System.Collections.Generic;
using System.Text;

namespace IIM.Shared.Dtos
{
	public class EntityGroupDto
	{
		public Guid GroupId { get; set; }
		public string Category { get; set; } // e.g., "User"
		public string Label { get; set; }    // e.g., "admin_account"

		// Just the summaries of the members
		public List<IndicatorSummary> Members { get; set; } = new();

		public float GroupConfidence { get; set; }
	}
}
