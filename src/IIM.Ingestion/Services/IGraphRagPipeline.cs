using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GraphRag.Community;
using GraphRag.Data;
using GraphRag.Entities;
using GraphRag.Relationships;
using IIM.Ingestion.Models;
using IIM.Shared.Models;
using GraphRagConfig = GraphRag.Config.GraphRagConfig;

namespace IIM.Ingestion.Services;

public interface IGraphRagPipeline
{
	/// <summary>
	/// Process documents through GraphRAG pipeline without document context.
	/// Entities will be extracted but not linked to a specific document in Neo4j.
	/// </summary>
	Task<GraphRagResult> ProcessAsync(
		IEnumerable<DocumentInput> documents,
		GraphRagConfig? config = null,
		CancellationToken ct = default);

	/// <summary>
	/// Process documents through GraphRAG pipeline with full document context.
	/// Creates Document node in Neo4j and links all extracted entities to it.
	/// </summary>
	Task<GraphRagResult> ProcessAsync(
		IEnumerable<DocumentInput> documents,
		string? documentId,
		Guid? workspaceId,
		Guid? virtualFileId,
		string? fileName,
		GraphRagConfig? config = null,
		CancellationToken ct = default);
}

