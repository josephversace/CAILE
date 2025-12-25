using System;
using System.Collections.Generic;
using System.Text;

namespace IIM.Shared.Dtos
{
	public sealed record WorkspaceEvidencePlan(
		bool UseQdrant,
		bool UseNeo4j,
		bool IncludeFiles,
		bool IncludeEntities,
		bool IncludeRelationships,
		bool IncludeTimeline,
		bool UseDeterministicSection,
		int QdrantTopK
	);

}
