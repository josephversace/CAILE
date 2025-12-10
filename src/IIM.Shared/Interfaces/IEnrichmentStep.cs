using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Models;
using IIM.Shared.Models.Core;

namespace IIM.Shared.Interfaces;

public interface IEnrichmentStep
{
	/// Unique step name (lowercase)
	string Name { get; }

	/// Decide if step should run for this file
	bool ShouldRun(StoredFile file, VirtualFile vfile);

	/// Perform work + populate EnrichmentState
	Task RunAsync(
		EnrichmentState state,
		StoredFile stored,
		VirtualFile vfile,
		CancellationToken ct);
}
