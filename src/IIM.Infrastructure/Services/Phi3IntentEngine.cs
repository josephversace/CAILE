using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ML.OnnxRuntimeGenAI;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;

namespace IIM.Infrastructure.AI.Intent
{
	public sealed class Phi3WorkspaceIntentEngine
		: IWorkspaceIntentEngine, IDisposable
	{
		private readonly Model _model;
		private readonly Tokenizer _tokenizer;

		public Phi3WorkspaceIntentEngine(string modelPath)
		{
			_model = new Model(modelPath);
			_tokenizer = new Tokenizer(_model);
		}

	

		public async Task<WorkspaceIntent> ClassifyAsync(IReadOnlyList<AGUIMessage> messages,IReadOnlyList<object> context,CancellationToken ct)
		{
			var lastUser = messages.LastOrDefault(m => m.Role == "user");
			if (lastUser is null || string.IsNullOrWhiteSpace(lastUser.Content))
				return WorkspaceIntent.Unknown;

			var prompt = BuildPrompt(lastUser.Content, context);

			Console.WriteLine($">>> INTENT PROMPT:\n{prompt}");

			using var sequences = _tokenizer.Encode(prompt);
			var inputTokenCount = sequences[0].Length;

			Console.WriteLine($">>> Input token count: {inputTokenCount}");

			using var genParams = new GeneratorParams(_model);
			genParams.SetSearchOption("max_length", inputTokenCount + 16); // Give it more room
			genParams.SetSearchOption("temperature", 0.0);
			genParams.SetSearchOption("top_k", 1);  // Should be int, not double
			genParams.SetSearchOption("do_sample", false);

			using var generator = new Generator(_model, genParams);
			generator.AppendTokenSequences(sequences);

			// Generate tokens
			var generated = new List<int>();
			for (int i = 0; i < 10 && !generator.IsDone(); i++)
			{
				generator.GenerateNextToken();
				var seq = generator.GetSequence(0);
				if (seq.Length > inputTokenCount)
				{
					var lastToken = seq[seq.Length - 1];
					generated.Add(lastToken);
					Console.WriteLine($">>> Token {i}: {lastToken}");
				}
			}

			var fullSequence = generator.GetSequence(0);
			var generatedSpan = fullSequence.Slice(inputTokenCount);

			Console.WriteLine($">>> Generated token IDs: [{string.Join(", ", generatedSpan.ToArray())}]");

			var rawOutput = _tokenizer.Decode(generatedSpan);
			Console.WriteLine($">>> Raw decoded: [{rawOutput}]");

			return rawOutput switch
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



		private static bool TryParseIntentJson(string json, out WorkspaceIntent intent)
		{
			intent = WorkspaceIntent.Unknown;
			try
			{
				// Extract JSON object (handle partial JSON)
				var jsonStart = json.IndexOf('{');
				var jsonEnd = json.LastIndexOf('}') + 1;
				if (jsonStart >= 0 && jsonEnd > jsonStart)
				{
					var jsonObj = json.Substring(jsonStart, jsonEnd - jsonStart);
					using var doc = JsonDocument.Parse(jsonObj);

					if (doc.RootElement.TryGetProperty("intent", out var intentProp))
					{
						return Enum.TryParse(intentProp.GetString(), true, out intent);
					}
				}
			}
			catch { }
			return false;
		}


		private static string ExtractIntentToken(string output)
		{
			output = output?.Trim() ?? string.Empty;

			// Find intent line and extract value after colon
			var intentLine = output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
								  .FirstOrDefault(line => line.StartsWith("Intent:", StringComparison.OrdinalIgnoreCase));

			if (intentLine != null)
			{
				var intentValue = intentLine.Split(':', 2)[1]?.Trim();
				if (!string.IsNullOrEmpty(intentValue))
					return intentValue;
			}

			// Fallback: first word after any colon
			var afterColon = output.Split(':').Skip(1).FirstOrDefault()?.Trim();
			return afterColon?.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries)
							 .FirstOrDefault() ?? string.Empty;
		}


		//		private static string BuildPrompt(string userMessage, IReadOnlyList<object> context)
		//		{
		//			return $"""
		//You are an intent classifier for a workspace-based analysis system.

		//Choose ONE value from the list below.
		//Return ONLY the value. No explanation.

		//Allowed values:
		//- FactLookup
		//- EntityInquiry
		//- RelationshipAnalysis
		//- TimelineAnalysis
		//- WorkspaceSummary
		//- HypothesisTesting

		//User message:
		//{userMessage}


		//""";
		//		}

		private static string BuildPrompt(string userMessage, IReadOnlyList<object> context)
		{
			// Phi-3 Instruct format
			return $"""
<|system|>
You are an intent classifier. Respond with exactly ONE word from this list:
FactLookup, EntityInquiry, RelationshipAnalysis, TimelineAnalysis, WorkspaceSummary, HypothesisTesting

Do not explain. Output only the intent label.<|end|>
<|user|>
{userMessage}<|end|>
<|assistant|>
""";
		}




		public void Dispose()
		{
			_tokenizer.Dispose();
			_model.Dispose();
		}
	}
}
