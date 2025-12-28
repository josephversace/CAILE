// ═══════════════════════════════════════════════════════════════════════════════
// WORKSPACE EVIDENCE PLAN
// ═══════════════════════════════════════════════════════════════════════════════

namespace IIM.Shared.Models;

/// <summary>
/// Plan for retrieving evidence based on intent classification.
/// This is POLICY - no models are called, no data is retrieved.
/// </summary>
/// <param name="UseQdrant">Whether to query vector store.</param>
/// <param name="UseNeo4j">Whether to query knowledge graph.</param>
/// <param name="IncludeFiles">Whether to include file metadata.</param>
/// <param name="IncludeEntities">Whether to retrieve entities.</param>
/// <param name="IncludeRelationships">Whether to retrieve relationships.</param>
/// <param name="IncludeTimeline">Whether to retrieve timeline events.</param>
/// <param name="UseDeterministicSection">Whether to use deterministic (non-semantic) retrieval.</param>
/// <param name="QdrantTopK">Number of chunks to retrieve from vector store.</param>
/// <param name="ModelId">Model identifier for context budget calculation.</param>
public sealed record WorkspaceEvidencePlan(
	bool UseQdrant,
	bool UseNeo4j,
	bool IncludeFiles,
	bool IncludeEntities,
	bool IncludeRelationships,
	bool IncludeTimeline,
	bool UseDeterministicSection,
	int QdrantTopK,
	string? ModelId = null
)
{
	/// <summary>
	/// Default plan for unknown intents.
	/// </summary>
	public static WorkspaceEvidencePlan Default => new(
		UseQdrant: true,
		UseNeo4j: false,
		IncludeFiles: true,
		IncludeEntities: false,
		IncludeRelationships: false,
		IncludeTimeline: false,
		UseDeterministicSection: false,
		QdrantTopK: 8
	);

	/// <summary>
	/// Plan for workspace summary requests.
	/// </summary>
	public static WorkspaceEvidencePlan WorkspaceSummary => new(
		UseQdrant: true,
		UseNeo4j: true,
		IncludeFiles: true,
		IncludeEntities: true,
		IncludeRelationships: true,
		IncludeTimeline: false,
		UseDeterministicSection: false,
		QdrantTopK: 12
	);

	/// <summary>
	/// Plan for entity-focused queries.
	/// </summary>
	public static WorkspaceEvidencePlan EntityFocused => new(
		UseQdrant: false,
		UseNeo4j: true,
		IncludeFiles: false,
		IncludeEntities: true,
		IncludeRelationships: true,
		IncludeTimeline: false,
		UseDeterministicSection: false,
		QdrantTopK: 0
	);

	/// <summary>
	/// Plan for timeline-focused queries.
	/// </summary>
	public static WorkspaceEvidencePlan TimelineFocused => new(
		UseQdrant: true,
		UseNeo4j: true,
		IncludeFiles: false,
		IncludeEntities: false,
		IncludeRelationships: false,
		IncludeTimeline: true,
		UseDeterministicSection: false,
		QdrantTopK: 8
	);

	/// <summary>
	/// Plan for single-file deterministic retrieval.
	/// </summary>
	public static WorkspaceEvidencePlan SingleFileDeterministic => new(
		UseQdrant: false,
		UseNeo4j: false,
		IncludeFiles: true,
		IncludeEntities: false,
		IncludeRelationships: false,
		IncludeTimeline: false,
		UseDeterministicSection: true,
		QdrantTopK: 0
	);
}
