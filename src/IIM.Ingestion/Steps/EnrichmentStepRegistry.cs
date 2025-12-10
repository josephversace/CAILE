using System.Collections.Generic;
using IIM.Shared.Interfaces;

namespace IIM.Ingestion.Steps;

public class EnrichmentStepRegistry : IEnrichmentStepRegistry
{
	public IEnumerable<IEnrichmentStep> Steps { get; }

	public EnrichmentStepRegistry(IEnumerable<IEnrichmentStep> steps)
	{
		Steps = steps;
	}
}
