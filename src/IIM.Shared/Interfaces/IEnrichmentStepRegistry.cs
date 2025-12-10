using System.Collections.Generic;

namespace IIM.Shared.Interfaces;

public interface IEnrichmentStepRegistry
{
	IEnumerable<IEnrichmentStep> Steps { get; }
}
