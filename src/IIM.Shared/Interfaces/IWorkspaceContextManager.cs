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
	Task<WorkspaceContext> BuildAsync(
		Guid workspaceId,
		IReadOnlyList<string> fileHashes,
		string userQuery,
		WorkspaceIntent intent,
		WorkspaceEvidencePlan plan,
		RetrievedContextCache cache,
		CancellationToken ct);
}

public sealed record RetrievedContextCache(
	IReadOnlySet<string> Chunks,
	IReadOnlySet<string> Entities,
	IReadOnlySet<string> Relationships
)
{
	public static RetrievedContextCache Empty => new(
		new HashSet<string>(),
		new HashSet<string>(),
		new HashSet<string>()
	);
}