using System;
using System.Collections.Generic;
using System.Text;
using GraphRag.Community;
using GraphRag.Entities;
using GraphRag.Relationships;
using GraphRag.Storage;

namespace IIM.Ingestion.Services
{
	public class AnalysisService
	{
		public async Task AnalyzeResultsAsync()
		{
			var storage = new FilePipelineStorage("output");

			// Load extracted entities
			var entities = await storage.LoadTableAsync<EntityRecord>("entities");
			foreach (var entity in entities.Take(10))
			{
				Console.WriteLine($"Entity: {entity.Title} ({entity.Type}) - {entity.Description}");
			}

			// Load relationships
			var relationships = await storage.LoadTableAsync<RelationshipRecord>("relationships");

			// Load communities
			var communities = await storage.LoadTableAsync<CommunityRecord>("communities");

			// Load community summaries
			var reports = await storage.LoadTableAsync<CommunityReportRecord>("community_reports");
			foreach (var report in reports)
			{
				Console.WriteLine($"Community {report.CommunityId}: {report.Summary}");
			}
		}
	}
}
