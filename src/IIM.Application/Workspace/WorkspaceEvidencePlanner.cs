using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Bibliography;
using IIM.Shared.Dtos;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;

namespace IIM.Application.Workspace
{
	public sealed class WorkspaceEvidencePlanner : IWorkspaceEvidencePlanner
	{
		private readonly IWorkspaceManager _workspaceManager;

        public WorkspaceEvidencePlanner(IWorkspaceManager workspaceManager)
        {
            _workspaceManager = workspaceManager;
        }

        public async Task<WorkspaceEvidencePlan> BuildPlan(WorkspaceIntent intent, IReadOnlyList<object> context, Guid? workspaceid, List<string?> filehashes)
		{
			// This method translates a high-level workspace intent into
			// a deterministic evidence retrieval plan.
			//
			// IMPORTANT:
			// - This is POLICY, not inference
			// - No models are called here
			// - No data is retrieved here
			// - The intent is treated as authoritative and immutable
			//
			// The returned plan controls:
			// - which backends may be queried (Qdrant, Neo4j)
			// - which categories of evidence are allowed
			// - how broad or narrow retrieval should be
			if (filehashes != null && filehashes.Count == 1)
			{
				var filehash = filehashes[0];
				if (!string.IsNullOrEmpty(filehash))
				{
					var result = await _workspaceManager.GetMetadataJsonAsync(
						filehash,
						processorName: "TextExtraction",
						latestOnly: true,
						CancellationToken.None);

					string? metadata = result[0];
					;
					if (!string.IsNullOrEmpty(metadata))
					{
						// ✅ We KNOW there is extracted text for exactly one file
						// → no vector search needed
						return new WorkspaceEvidencePlan(
							UseQdrant: false,
							UseNeo4j: false,
							IncludeFiles: true,
							IncludeEntities: false,
							IncludeRelationships: false,
							IncludeTimeline: false,
							QdrantTopK: 0,
							UseDeterministicSection: true
						);
					}
				}
			}



			return intent switch
			{
				// User wants a high-level understanding of the workspace.
				// We allow:
				// - semantic chunks (Qdrant)
				// - entities + relationships (Neo4j)
				// We do NOT include timeline by default to avoid noise.
				WorkspaceIntent.WorkspaceSummary =>
					new WorkspaceEvidencePlan(true, true, true, true, true, false, false, 12),

				// User is explicitly asking about people, organizations, or concepts.
				// We skip semantic text retrieval and focus on the graph.
				WorkspaceIntent.EntityInquiry =>
					new WorkspaceEvidencePlan(false, true, false, true, true, false, false, 0),

				// User wants a chronological view.
				// We include timeline events and supporting semantic context,
				// but skip entity expansion to keep the output focused.
				WorkspaceIntent.TimelineAnalysis =>
					new WorkspaceEvidencePlan(true, true, false, false, false, true, false, 8),

				// Default fallback for ambiguous or unknown intents.
				// We take a conservative approach:
				// - limited semantic context
				// - no graph expansion
				// - safe for general Q&A
				_ =>
					new WorkspaceEvidencePlan(true, false, true, false, false, false, false, 6)
			};
		}

		private static bool AsksForLatest(IReadOnlyList<object> context)
		{
			var userText = context
				.OfType<AGUIMessage>()
				.LastOrDefault(m => m.Role == "user")?.Content;

			if (string.IsNullOrWhiteSpace(userText))
				return false;

			return userText.Contains("latest", StringComparison.OrdinalIgnoreCase)
				|| userText.Contains("most recent", StringComparison.OrdinalIgnoreCase)
				|| userText.Contains("newest", StringComparison.OrdinalIgnoreCase);
		}

	}

}
