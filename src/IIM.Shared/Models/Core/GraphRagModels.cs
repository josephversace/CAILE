using System;
using System.Collections.Generic;
using System.Text;
using GraphRag.Data;

namespace IIM.Shared.Models
{
	public record DocumentInput(string FileName, object Content); // Content: byte[], Stream, or string

	public class GraphRagResult
	{
		public IReadOnlyList<GraphRag.Entities.EntityRecord> Entities { get; init; } = [];
		public IReadOnlyList<GraphRag.Relationships.RelationshipRecord> Relationships { get; init; } = [];
		public IReadOnlyList<GraphRag.Community.CommunityRecord> Communities { get; init; } = [];
		public IReadOnlyList<GraphRag.Community.CommunityReportRecord> CommunityReports { get; init; } = [];
		public IReadOnlyList<TextUnitRecord> TextUnits { get; init; } = [];
		public IReadOnlyList<DocumentRecord> Documents { get; init; } = [];
		public IReadOnlyList<Exception> Errors { get; init; } = [];
	}
}
