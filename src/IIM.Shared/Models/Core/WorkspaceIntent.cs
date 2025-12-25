using System;
using System.Collections.Generic;
using System.Text;

namespace IIM.Shared.Models
{
	public enum WorkspaceIntent
	{
		Unknown = 0,
		FactLookup,
		EntityInquiry,
		RelationshipAnalysis,
		TimelineAnalysis,
		WorkspaceSummary,     
		HypothesisTesting
	}

}
