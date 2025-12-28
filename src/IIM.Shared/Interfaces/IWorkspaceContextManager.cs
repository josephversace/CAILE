// src/IIM.Shared/Interfaces/IWorkspaceContextManager.cs
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Dtos;
using IIM.Shared.Models;

namespace IIM.Shared.Interfaces;

public interface IWorkspaceContextManager
{
	/// <summary>
	/// Build context for a workspace query.
	/// </summary>
	/// <param name="workspaceId">The workspace ID (may be empty if using file hashes).</param>
	/// <param name="fileHashes">Specific file hashes to include.</param>
	/// <param name="userQuery">The user's query text.</param>
	/// <param name="intent">Classified intent.</param>
	/// <param name="plan">Retrieval plan.</param>
	/// <param name="cache">Previously retrieved context to avoid duplication.</param>
	/// <param name="ct">Cancellation token.</param>
	Task<WorkspaceContext> BuildAsync(
		Guid workspaceId,
		IReadOnlyList<string> fileHashes,
		string userQuery,
		WorkspaceIntent intent,
		WorkspaceEvidencePlan plan,
		RetrievedContextCache cache,
		CancellationToken ct);
}

