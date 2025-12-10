using System;
using System.Collections.Generic;
using System.Text;
using GraphRag.Graphs;

namespace IIM.Ingestion.Services
{
	public class GraphService(IGraphStore graphStore)
	{
		public async Task AddEntitiesAsync(CancellationToken ct)
		{
			// Initialize the store
			await graphStore.InitializeAsync(ct);

			// Add nodes
			await graphStore.UpsertNodeAsync(
				id: "person-1",
				label: "Person",
				properties: new Dictionary<string, object?>
				{
					["name"] = "Alice",
					["role"] = "Engineer"
				},
				ct);

			// Add relationships
			await graphStore.UpsertRelationshipAsync(
				sourceId: "person-1",
				targetId: "org-1",
				type: "WORKS_AT",
				properties: new Dictionary<string, object?> { ["since"] = "2020" },
				ct);

			// Bulk operations
			await graphStore.UpsertNodesAsync([
				new GraphNodeUpsert("org-1", "Organization", new Dictionary<string, object?> { ["name"] = "Acme" }),
			new GraphNodeUpsert("org-2", "Organization", new Dictionary<string, object?> { ["name"] = "TechCo" })
			], ct);

			// Query relationships
			await foreach (var rel in graphStore.GetOutgoingRelationshipsAsync("person-1", ct))
			{
				Console.WriteLine($"{rel.SourceId} --[{rel.Type}]--> {rel.TargetId}");
			}

			// Traverse all nodes with pagination
			var options = new GraphTraversalOptions { Skip = 0, Take = 100 };
			await foreach (var node in graphStore.GetNodesAsync(options, ct))
			{
				Console.WriteLine($"Node: {node.Id} ({node.Label})");
			}
		}
	}
}
