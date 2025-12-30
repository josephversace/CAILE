using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using OllamaSharp;
using OllamaSharp.Models;

namespace IIM.Infrastructure.AI.Intent
{
	public sealed class OllamaWorkspaceIntentEngine : IWorkspaceIntentEngine
	{
		private readonly OllamaApiClient _client;
		private readonly string _modelId;

		public OllamaWorkspaceIntentEngine(string endpoint, string modelId)
		{
			_client = new OllamaApiClient(new Uri(endpoint));
			_modelId = modelId;
		}

		public async Task<WorkspaceIntent> ClassifyAsync(
			IReadOnlyList<AGUIMessage> messages,
			IReadOnlyList<object> context,
			CancellationToken ct)
		{
			var lastUser = messages.LastOrDefault(m => m.Role == "user");
			if (lastUser is null || string.IsNullOrWhiteSpace(lastUser.Content))
				return WorkspaceIntent.Unknown;

			var prompt = BuildPrompt(lastUser.Content);

			var request = new GenerateRequest
			{
				Model = _modelId,
				Prompt = prompt,
				Stream = false,
				Options = new RequestOptions
				{
					Temperature = 0.0f,
					NumPredict = 20,  // Intent label is short
					TopK = 1
				}
			};

			try
			{
				var response = await _client
					.GenerateAsync(request, ct)
					.ToListAsync(ct);

				var output = string.Join("", response.Select(r => r.Response)).Trim();

				return ParseIntent(output);
			}
			catch (Exception)
			{
				return WorkspaceIntent.Unknown;
			}
		}

		private static WorkspaceIntent ParseIntent(string output)
		{
			if (string.IsNullOrWhiteSpace(output))
				return WorkspaceIntent.Unknown;

			// Clean up the output - take first word/line
			var cleaned = output
				.Split(new[] { '\n', '\r', ' ', '.', ',', ':' }, StringSplitOptions.RemoveEmptyEntries)
				.FirstOrDefault()?
				.Trim();

			return cleaned switch
			{
				"FactLookup" => WorkspaceIntent.FactLookup,
				"EntityInquiry" => WorkspaceIntent.EntityInquiry,
				"RelationshipAnalysis" => WorkspaceIntent.RelationshipAnalysis,
				"TimelineAnalysis" => WorkspaceIntent.TimelineAnalysis,
				"WorkspaceSummary" => WorkspaceIntent.WorkspaceSummary,
				"HypothesisTesting" => WorkspaceIntent.HypothesisTesting,
				_ => WorkspaceIntent.Unknown
			};
		}

		private static string BuildPrompt(string userMessage)
		{
			return $"""
                You are an intent classifier. Respond with exactly ONE word from this list:
                FactLookup, EntityInquiry, RelationshipAnalysis, TimelineAnalysis, WorkspaceSummary, HypothesisTesting

                Do not explain. Output only the intent label.

                User message: {userMessage}

                Intent:
                """;
		}
	}
}