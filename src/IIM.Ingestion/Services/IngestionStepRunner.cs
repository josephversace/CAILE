using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Models;
using Microsoft.Extensions.Logging;

namespace IIM.Ingestion.Services
{
	public sealed class IngestionStepRunner
	{
		private readonly IReadOnlyDictionary<string, IIngestionStep> _steps;
		private readonly ILogger<IngestionStepRunner> _logger;

		public IngestionStepRunner(IEnumerable<IIngestionStep> steps, ILogger<IngestionStepRunner> logger)
		{
			_steps = steps.ToDictionary(s => s.Id, StringComparer.OrdinalIgnoreCase);
			_logger = logger;
		}

		public async Task RunAsync(IngestionStepContext ctx, IngestionRunOptions options, CancellationToken ct)
		{
			options ??= IngestionRunOptions.Default;


			var plan = BuildPlan(options);

			if (plan.Any(id => _steps[id].RequiresBytes))
				_ = await ctx.GetBytesAsync(ct);


			foreach (var stepId in plan)
			{
				if (ctx.StopCts?.IsCancellationRequested == true)
				{
					_logger.LogInformation("Ingestion stop requested; halting remaining steps.");
					break;
				}


				if (!_steps.TryGetValue(stepId, out var step))
					throw new InvalidOperationException("Unknown step: " + stepId);

				var (inputHash, parametersHash) = await step.GetIdentityAsync(ctx, ct);

				var existing = await ctx.Workspace.GetStepAsync(
					ctx.StoredFile.Blake3Hash,
					step.Id,
					step.Version,
					inputHash,
					parametersHash,
					ct);

				if (!options.Force && existing?.Status == IngestionStepStatus.Completed)
				{
					var ok = await step.VerifyAsync(ctx, existing.OutputHash, ct);
					if (ok)
					{
						_logger.LogDebug("Step {StepId} skipped (completed + verified)", step.Id);
						continue;
					}

					// mark inconsistent and re-run
					await ctx.Workspace.UpsertStepAsync(new IngestionStepState
					{
						Id = existing.Id,
						StoredFileHash = existing.StoredFileHash,
						WorkspaceId = existing.WorkspaceId,
						VirtualFileId = existing.VirtualFileId,
						StepId = existing.StepId,
						StepVersion = existing.StepVersion,
						InputHash = existing.InputHash,
						ParametersHash = existing.ParametersHash,
						OutputHash = existing.OutputHash,
						MetadataJson = existing.MetadataJson,
						Status = IngestionStepStatus.Inconsistent,
						AttemptCount = existing.AttemptCount,
						IsFatal = existing.IsFatal,
						IsDeferred = existing.IsDeferred,
						CreatedAt = existing.CreatedAt,
						UpdatedAt = DateTimeOffset.UtcNow
					}, ct);
				}

				var row = await ctx.Workspace.UpsertStepAsync(new IngestionStepState
				{
					StoredFileHash = ctx.StoredFile.Blake3Hash,
					WorkspaceId = ctx.VirtualFile.WorkspaceId,
					VirtualFileId = ctx.VirtualFile.Id,
					StepId = step.Id,
					StepVersion = step.Version,
					InputHash = inputHash,
					ParametersHash = parametersHash,
					Status = IngestionStepStatus.Pending,
					IsFatal = step.IsFatal,
					IsDeferred = false
				}, ct);

				await ctx.Workspace.MarkStepRunningAsync(row.Id, ct);

				try
				{
					_logger.LogInformation("Running step {StepId} v{Version}", step.Id, step.Version);

					var (outputHash, metadataJson) = await step.ExecuteAsync(ctx, ct);

					await ctx.Workspace.MarkStepCompletedAsync(row.Id, outputHash, metadataJson, ct);
				}
				catch (Exception ex)
				{
					await ctx.Workspace.MarkStepFailedAsync(row.Id, ex.ToString(), step.IsFatal, ct);
					if (!options.ContinueOnError || step.IsFatal)
						throw;
				}
			}
		}

		// Deterministic plan + "AI + Excel late"
		private List<string> BuildPlan(IngestionRunOptions options)
		{
			var defaultsOrdered = new List<string>
			{
				IngestionStepIds.MetaExifFast,
				IngestionStepIds.DocExtractText,
				IngestionStepIds.DocShapeDetect,
				IngestionStepIds.IocRegexExtract,
				IngestionStepIds.ChunkBuild,
				IngestionStepIds.EmbedIndexQdrant,
				IngestionStepIds.AiTextAnalysis,
				IngestionStepIds.AiImageDescribe,
				IngestionStepIds.ExcelStructureDetect,
				IngestionStepIds.ExcelCanonicalize,
			};

			HashSet<string> selected;

			if (options.OnlySteps != null && options.OnlySteps.Count > 0)
				selected = new HashSet<string>(options.OnlySteps, StringComparer.OrdinalIgnoreCase);
			else
				selected = new HashSet<string>(defaultsOrdered, StringComparer.OrdinalIgnoreCase);

			if (options.AdditionalSteps != null)
				foreach (var s in options.AdditionalSteps)
					selected.Add(s);

			if (options.IncludeDependencies)
				selected = ExpandDependencies(selected);

			return TopoSortStable(selected, defaultsOrdered);

			HashSet<string> ExpandDependencies(HashSet<string> set)
			{
				var closure = new HashSet<string>(set, StringComparer.OrdinalIgnoreCase);
				var stack = new Stack<string>(set);

				while (stack.Count > 0)
				{
					var id = stack.Pop();
					if (!_steps.TryGetValue(id, out var step))
						continue;

					foreach (var dep in step.DependsOn)
						if (closure.Add(dep))
							stack.Push(dep);
				}

				return closure;
			}

			List<string> TopoSortStable(HashSet<string> set, List<string> preferredOrder)
			{
				// stable ordering:
				// 1) in preferredOrder order
				// 2) anything else alphabetical
				var orderIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
				for (int i = 0; i < preferredOrder.Count; i++)
					orderIndex[preferredOrder[i]] = i;

				var nodes = set
					.OrderBy(id => orderIndex.TryGetValue(id, out var idx) ? idx : int.MaxValue)
					.ThenBy(id => id, StringComparer.OrdinalIgnoreCase)
					.ToList();

				var result = new List<string>();
				var temp = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				var perm = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

				void Visit(string n)
				{
					if (!set.Contains(n)) return;
					if (perm.Contains(n)) return;
					if (temp.Contains(n)) throw new InvalidOperationException("Step dependency cycle at: " + n);

					temp.Add(n);

					if (_steps.TryGetValue(n, out var step))
					{
						foreach (var dep in step.DependsOn)
							Visit(dep);
					}

					temp.Remove(n);
					perm.Add(n);
					result.Add(n);
				}

				foreach (var n in nodes) Visit(n);
				return result;
			}
		}
	}
}
