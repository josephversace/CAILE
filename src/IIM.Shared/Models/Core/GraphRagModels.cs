using System;
using System.Collections.Generic;
using System.Text;
using GraphRag.Community;
using GraphRag.Data;
using GraphRag.Entities;
using GraphRag.Relationships;

namespace IIM.Shared.Models
{
	public record DocumentInput(string FileName, object Content);

	public record GraphRagResult
	{
		public IReadOnlyList<TextUnitRecord> TextUnits { get; init; } = [];
		public IReadOnlyList<DocumentRecord> Documents { get; init; } = [];
		public IReadOnlyList<EntityRecord> Entities { get; init; } = [];
		public IReadOnlyList<RelationshipRecord> Relationships { get; init; } = [];
		public IReadOnlyList<CommunityRecord> Communities { get; init; } = [];
		public IReadOnlyList<CommunityReportRecord> CommunityReports { get; init; } = [];
		public int Neo4jNodeCount { get; init; }
		public int Neo4jRelationshipCount { get; init; }
		public IReadOnlyList<Exception> Errors { get; init; } = [];

		public bool HasErrors => Errors.Count > 0;
		public bool IsEmpty => Entities.Count == 0;
	}
}
