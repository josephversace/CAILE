using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using IIM.Api.Services;
using IIM.Infrastructure.Services;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using IIM.Shared.Models.Configuration;
using IIM.Shared.Models.Core;
using MagikaSharp;
using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using OpenAI.Assistants;
using Org.BouncyCastle.Ocsp;



namespace IIM.Api.Endpoints;

public static class AIEndpoints
{
	public static void MapAIEndpoints(this WebApplication app)
	{
		app.MapPost("/ai/chat-ui", HandleChatAsync);
		app.MapPost("/ai/reason-ui", HandleReasoningAsync);
		app.MapPost("/ai/reload-models", ReloadModelsAsync);
		app.MapGet("/ai/models", GetModelsAsync);
	}



	// ============================================================
	// AG-UI CHAT HANDLER (Stable with your current SDK)
	// ============================================================
	private static async Task HandleChatAsync(
		HttpContext ctx,
		IAIAgentFactory agentFactory,
		IWorkspaceEvidencePlanner evidencePlanner,
		IWorkspaceContextManager contextManager,
		IToolRegistry tools,
		PromptResolver promptResolver,
		IPromptSnapshotProvider promptSnapshot)
	{
		await RunAgentAsync(ctx,agentFactory.GetChatAgentAsync,evidencePlanner, contextManager, tools, promptResolver, promptSnapshot);
	}

	private static async Task HandleReasoningAsync(
		HttpContext ctx,
		IAIAgentFactory agentFactory,
		IWorkspaceEvidencePlanner evidencePlanner,
		IWorkspaceContextManager contextManager,
		IToolRegistry tools,
		PromptResolver promptResolver,
		IPromptSnapshotProvider promptSnapshot)
	{
		await RunAgentAsync(ctx,agentFactory.GetReasoningAgentAsync,evidencePlanner,contextManager,tools,promptResolver,promptSnapshot);
	}

	// ============================================================
	// CORE EXECUTION PIPELINE (Handles streaming + tools + errors)
	// ============================================================
	private static async Task RunAgentAsync(
		HttpContext ctx,
		Func<AgentExecutionContext?, Task<AIAgent>> agentResolver,
		IWorkspaceEvidencePlanner agentEvidencePlanner,
		IWorkspaceContextManager workspaceContext,
		IToolRegistry toolRegistry,
		PromptResolver promptResolver,
		IPromptSnapshotProvider promptSnapshotProvider)
	{
		// SSE headers
		ctx.Response.Headers["Content-Type"] = "text/event-stream";
		ctx.Response.Headers["Cache-Control"] = "no-cache, no-transform";
		ctx.Response.Headers["Connection"] = "keep-alive";
		ctx.Response.Headers["X-Accel-Buffering"] = "no";

		var abort = ctx.RequestAborted;

		// Read raw body first to debug
		ctx.Request.EnableBuffering();
		using var reader = new StreamReader(ctx.Request.Body);
		var rawBody = await reader.ReadToEndAsync();
		Console.WriteLine($">>> 2. Raw body: [{rawBody}]");

		// Reset stream position for deserialization
		ctx.Request.Body.Position = 0;

		AGUIRequest? req = null;
		WorkspaceContext? wsContext = null;

		var promptSnapshot = await promptSnapshotProvider.GetSnapshotAsync(ct: abort);


		try
		{
			req = await JsonSerializer.DeserializeAsync<AGUIRequest>(
				ctx.Request.Body,
				new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
				cancellationToken: abort);

			Console.WriteLine($">>> 3. Deserialized - ThreadId: {req?.ThreadId}, Messages: {req?.Messages?.Count}");

			if (req?.Messages != null)
			{
				foreach (var m in req.Messages)
				{
					Console.WriteLine($">>>    Message - Role: [{m.Role}], Content: [{m.Content}]");
				}
			}

			var (workspaceId, fileHashes) = ExtractContextChips(req);
			
			var cache = ExtractCache(req);

			
			if (workspaceId != Guid.Empty || fileHashes.Count > 0)
			{
				var intentEngine = ctx.RequestServices.GetRequiredService<IWorkspaceIntentEngine>();
				var intent = await intentEngine.ClassifyAsync(req.Messages, req.Context, abort);
				var plan = await agentEvidencePlanner.BuildPlan(intent, req.Context, workspaceId, fileHashes);

				var lastMessage = req.Messages.LastOrDefault(m => m.Role == "user");

				wsContext = await workspaceContext.BuildAsync(workspaceId,fileHashes,lastMessage?.Content ?? "", intent, plan, cache, abort);
			}



		}
		catch (Exception ex)
		{
			Console.WriteLine($">>> 3. Deserialization FAILED: {ex.Message}");
			await ctx.Response.WriteAsync($"data: {{\"error\":\"{ex.Message}\"}}\n\n");
			return;
		}



		await ctx.Response.StartAsync();

	
		if (req == null || req.Messages.Count == 0)
		{
			await WriteEvent(ctx, new { type = "RUN_FINISHED" }, abort);
			return;
		}

		var userMsg = req.Messages.LastOrDefault(m => m.Role == "user");

		if (userMsg == null)
		{
			await WriteEvent(ctx, new { type = "RUN_FINISHED" }, abort);
			return;
		}

		var router =
		ctx.RequestServices.GetRequiredService<IToolRoutingService>();

		var toolDecision = await router.DecideAsync(userMsg.Content, allowWebSearch: req.Capabilities?.EnableWebSearch == true, abort);


		string result = "";

		if (wsContext is null)
		{

			if (string.Equals(toolDecision.ToolName, "no_tool", StringComparison.OrdinalIgnoreCase)
				|| !toolDecision.ShouldCallTool)
			{
				// Conversational fallthrough - no context injection needed, let the model respond naturally
				// Don't add a system message at all, or add a minimal one:
				// result = "CONTEXT: General conversation. Respond naturally.";
			}
			else if (toolDecision.ShouldCallTool && !string.IsNullOrEmpty(toolDecision.ToolName))
			{
				var args = toolDecision.Arguments.HasValue
					? JsonSerializer.Deserialize<Dictionary<string, object?>>(
						toolDecision.Arguments.Value.GetRawText())
					: null;

				result = await toolRegistry.InvokeAsync(toolDecision.ToolName!, args);

				req.Messages.Add(new AGUIMessage
				{
					Role = "system",
					Content = $"TOOL_RESULT: {result}\nSOURCE: {toolDecision.ToolName}"
				});
			}

		}

		// Load agent + create thread
		var agent = await agentResolver(
		new AgentExecutionContext
		{
			ModelOverrides = req.ModelOverrides
		});


		var resolvedPrompt = promptResolver.Resolve(
	snapshot: promptSnapshot,
	explicitPrompt: null, 
	overrideKey: wsContext?.PromptProfileKey, 
	defaultKey: "chat.default"
);



		var thread = agent.GetNewThread();

		if (req.Messages.Count == 0)
		{
			await WriteEvent(ctx, new { type = "RUN_FINISHED" }, abort);
			return;
		}


		//string prompt = BuildAugmentedPrompt(req.Messages, wsContext);

		var cleanMessages = BuildAugmentedPrompt(req.Messages, wsContext, resolvedPrompt.Content);

		var telemetry = new RunTelemetry
		{
			PromptCharCount = cleanMessages.Length,
			ContextTokenEstimate = wsContext?.TotalTokenEstimate ?? 0
		};


		string messageId = $"msg_{Guid.NewGuid():N}";


		// RUN_STARTED
		await WriteEvent(ctx, new
		{
			threadId = req.ThreadId,
			runId = req.RunId,
			type = "RUN_STARTED"
		}, abort);

		// TEXT_MESSAGE_START
		await WriteEvent(ctx, new
		{
			messageId,
			role = "assistant",
			type = "TEXT_MESSAGE_START"
		}, abort);



		var requestAbort = ctx.RequestAborted;

		// Give the model its own lifetime
		using var modelCts = CancellationTokenSource.CreateLinkedTokenSource(
			CancellationToken.None // ← important
		);

		// Still observe disconnects, but don't die instantly
		requestAbort.Register(() =>
		{
			// optional logging
		});

		// Optional hard safety timeout
		modelCts.CancelAfter(TimeSpan.FromMinutes(5));


		try
		{
			await foreach (var update in agent.RunStreamingAsync(cleanMessages, thread).WithCancellation(modelCts.Token))
			{
				foreach (var content in update.Contents)
				{

					if (content is TextContent text)
					{
						string t = text.Text;

						telemetry.AddCompletionText(t);

						// Regular text delta
						await WriteEvent(ctx, new
						{
							messageId,
							delta = t,
							type = "TEXT_MESSAGE_CONTENT"
						}, abort);

						await ctx.Response.Body.FlushAsync(abort);

					}
				}


			}
		}
		catch (TaskCanceledException te)
		{
			// Streaming canceled by client or timeout — expected
		}

		catch (OperationCanceledException ce)
		{
			// ignore — client disconnected
		}
		catch (Exception ex)
		{
			await WriteEvent(ctx, new
			{
				messageId,
				delta = $"Error: {ex.Message}",
				type = "TEXT_MESSAGE_CONTENT"
			}, abort);
		}

		// TEXT_MESSAGE_END
		await WriteEvent(ctx, new { messageId, type = "TEXT_MESSAGE_END" }, abort);

		// RUN_FINISHED
		await WriteEvent(ctx, new
		{
			threadId = req.ThreadId,
			runId = req.RunId,
			type = "RUN_FINISHED",

			telemetry = new
			{
				promptTokens = telemetry.PromptCharCount / 4,
				completionTokens = telemetry.CompletionTokenEstimate,
				tokensPerSecond = Math.Round(telemetry.TokensPerSecond, 2),
				contextTokens = telemetry.ContextTokenEstimate
			},

			newRetrievedChunks = wsContext?.NewChunkIds ?? [],
			newRetrievedEntities = wsContext?.NewEntityIds ?? [],
			newRetrievedRelationships = wsContext?.NewRelationshipIds ?? []
		}, abort);

	}


	private static async Task WriteEvent(HttpContext ctx, object data, CancellationToken ct)
	{
		if (!ctx.Response.Body.CanWrite)
			return;

		try
		{
			var json = JsonSerializer.Serialize(data);
			await ctx.Response.WriteAsync($"data: {json}\n\n");  // Remove "event: message\n"
			await ctx.Response.Body.FlushAsync();
		}
		catch (IOException)
		{
			// client gone — ignore
		}
	}


	// ============================================================
	// /ai/reload-models
	// ============================================================
	private static async Task<IResult> ReloadModelsAsync(IAIAgentFactory factory)
	{
		await factory.ReloadModelsAsync();
		return Results.Ok(new
		{
			success = true,
			factory.CurrentChatModel,
			factory.CurrentReasoningModel
		});
	}

	// ============================================================
	// /ai/models
	// ============================================================
	private static IResult GetModelsAsync(IAIAgentFactory factory)
	{
		return Results.Ok(new
		{
			chatModel = factory.CurrentChatModel,
			reasoningModel = factory.CurrentReasoningModel
		});
	}

	// ============================================================
	// CONTEXT EXTRACTION
	// ============================================================


	private static (Guid WorkspaceId, List<string> FileHashes) ExtractContextChips(AGUIRequest req)
	{
		Guid workspaceId = Guid.Empty;
		var fileHashes = new List<string>();

		if (req.Context == null)
			return (workspaceId, fileHashes);

		foreach (var item in req.Context)
		{
			if (item is not JsonElement je)
				continue;

			if (!je.TryGetProperty("type", out var typeProp))
				continue;

			var type = typeProp.GetString();

			if (type == "workspace" && je.TryGetProperty("id", out var wsIdProp))
			{
				if (Guid.TryParse(wsIdProp.GetString(), out var wsId))
					workspaceId = wsId;
			}
			else if (type == "file" && je.TryGetProperty("id", out var fileIdProp))
			{
				var hash = fileIdProp.GetString();
				if (!string.IsNullOrEmpty(hash))
					fileHashes.Add(hash);
			}
		}

		return (workspaceId, fileHashes);
	}

	private static RetrievedContextCache ExtractCache(AGUIRequest req)
	{
		return new RetrievedContextCache(
			Chunks: new HashSet<string>(req.RetrievedChunks ?? []),
			Entities: new HashSet<string>(req.RetrievedEntities ?? []),
			Relationships: new HashSet<string>(req.RetrievedRelationships ?? [])
		);
	}

	private static List<ChatMessage> BuildCleanChatMessages(List<AGUIMessage> messages, WorkspaceContext? ctx)
	{
		var chatMessages = new List<ChatMessage>();

		var systemBuilder = new StringBuilder();
		systemBuilder.AppendLine("### ROLE");
		systemBuilder.AppendLine("You are a professional investigative analyst.");

		systemBuilder.AppendLine("\n### TASK");
		systemBuilder.AppendLine("Summarize provided documents. No Chain-of-Thought. No LaTeX. Final answer only.");

		if (ctx != null && ctx.SemanticChunks.Any())
		{
			systemBuilder.AppendLine("\n### CONTEXT DOCUMENTS");
			foreach (var chunk in ctx.SemanticChunks.Take(5))
			{
				systemBuilder.AppendLine($"<DOC name=\"{chunk.FileName}\">");
				systemBuilder.AppendLine(chunk.Text);
				systemBuilder.AppendLine("</DOC>");
			}
		}

		chatMessages.Add(new ChatMessage(ChatRole.System, systemBuilder.ToString()));

		// Add user messages...
		return chatMessages;
	}


	private static string BuildToolResultBlock(string toolName, object result)
	{
		return $"""
    <tool_result name="{toolName}">
    {JsonSerializer.Serialize(result)}
    </tool_result>
    """;
	}



	private static string BuildAugmentedPrompt(List<AGUIMessage> messages,WorkspaceContext? ctx,string systemPrompt)
	{ 
		if (ctx == null) {

			return string.Join("\n", messages.Select(m => $"<|{m.Role}|>\n{m.Content}"));
		}

		var sb = new StringBuilder();

		// SYSTEM PROMPT (from resolver)
		sb.AppendLine(systemPrompt);
		sb.AppendLine();


		// System context block
		sb.AppendLine("<context>");

		if (ctx.SemanticChunks.Count > 0)
		{
			sb.AppendLine("<relevant_documents>");
			foreach (var chunk in ctx.SemanticChunks.Take(5))
			{
				sb.AppendLine($"[{chunk.FileName ?? "unknown"}] {chunk.Text}");
			}
			sb.AppendLine("</relevant_documents>");
		}

		if (ctx.Entities.Count > 0)
		{
			sb.AppendLine("<entities>");
			foreach (var e in ctx.Entities.Take(20))
			{
				sb.AppendLine($"- {e.Name} ({e.Type})");
			}
			sb.AppendLine("</entities>");
		}

		if (ctx.Relationships.Count > 0)
		{
			sb.AppendLine("<relationships>");
			foreach (var r in ctx.Relationships.Take(20))
			{
				sb.AppendLine($"- {r.SourceId} --[{r.Type}]--> {r.TargetId}");
			}
			sb.AppendLine("</relationships>");
		}

	

		if (ctx.Timeline.Count > 0)
		{
			sb.AppendLine("<timeline>");
			foreach (var t in ctx.Timeline.OrderByDescending(t => t.Timestamp).Take(10))
			{
				sb.AppendLine($"- [{t.Timestamp:yyyy-MM-dd HH:mm}] {t.EventType}: {t.Description}");
			}
			sb.AppendLine("</timeline>");
		}

		sb.AppendLine("</context>");
		sb.AppendLine();

		// Conversation history
		foreach (var msg in messages)
		{
			sb.AppendLine($"<|{msg.Role}|>");
			sb.AppendLine(msg.Content);
		}

		return sb.ToString();
	}
}

