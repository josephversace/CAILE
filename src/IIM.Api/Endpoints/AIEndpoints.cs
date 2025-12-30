using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using IIM.Infrastructure.Services;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using MagikaSharp;
using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
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
		IToolRegistry tools)
	{


		//await RunAgentAsync(ctx, agentFactory.GetChatAgentAsync, evidencePlanner, contextManager, tools);

		await RunAgentAsync(ctx, agentFactory.GetChatAgentAsync, evidencePlanner, contextManager, tools);
	}

	private static async Task HandleReasoningAsync(
		HttpContext ctx,
		IAIAgentFactory agentFactory,
		IWorkspaceEvidencePlanner evidencePlanner,
		IWorkspaceContextManager contextManager,
		IToolRegistry tools)
	{
		await RunAgentAsync(ctx, agentFactory.GetReasoningAgentAsync, evidencePlanner, contextManager, tools);
	}

	// ============================================================
	// CORE EXECUTION PIPELINE (Handles streaming + tools + errors)
	// ============================================================
	private static async Task RunAgentAsync(
		HttpContext ctx,
		Func<Task<AIAgent>> agentResolver,
		IWorkspaceEvidencePlanner agentEvidencePlanner,
		IWorkspaceContextManager workspaceContext,
		IToolRegistry toolRegistry)
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

		// Load agent + create thread
		var agent = await agentResolver();


		var thread = agent.GetNewThread();

		if (req.Messages.Count == 0)
		{
			await WriteEvent(ctx, new { type = "RUN_FINISHED" }, abort);
			return;
		}


		//string prompt = BuildAugmentedPrompt(req.Messages, wsContext);

		var cleanMessages = BuildAugmentedPrompt(req.Messages, wsContext);

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

		// Streaming loop
		bool insideTool = false;
		var toolBuffer = new StringBuilder();

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

						// ================ TOOL-CALL DETECTION ==================
						if (!insideTool)
						{
							if (t.Contains("<tool_call>"))
							{
								insideTool = true;
								toolBuffer.Append(t);
								continue;
							}

							// Regular text delta
							await WriteEvent(ctx, new
							{
								messageId,
								delta = t,
								type = "TEXT_MESSAGE_CONTENT"
							}, abort);

							await ctx.Response.Body.FlushAsync(abort);
						}
						else
						{
							// Collect tool-call XML chunk-by-chunk
							toolBuffer.Append(t);

							if (toolBuffer.ToString().Contains("</tool_call>"))
							{
								insideTool = false;
								string xml = toolBuffer.ToString();
								toolBuffer.Clear();

								ToolCall call = toolRegistry.TryParseToolCall(xml);
								if (call != null)
								{
									await ExecuteToolCallAsync(
										ctx, call, toolRegistry,
										agent, thread,
										messageId, "", abort
									);
								}
							}
						}
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
			result = (object?)null,
			newRetrievedChunks = wsContext?.NewChunkIds ?? [],
			newRetrievedEntities = wsContext?.NewEntityIds ?? [],
			newRetrievedRelationships = wsContext?.NewRelationshipIds ?? []
		}, abort);
	}




	// ============================================================
	// TOOL CALL EXECUTION (Stable for all models: Qwen, Phi, Llama)
	// ============================================================
	private static async Task ExecuteToolCallAsync(
		HttpContext ctx,
		ToolCall call,
		IToolRegistry registry,
		AIAgent agent,
		AgentThread thread,
		string messageId,
		string userPrompt,
		CancellationToken abort)
	{
		try
		{
			// Execute tool
			var result = await registry.InvokeAsync(call.Name, call.Arguments);

			// Build follow-up prompt manually
			string followup = $"""
            The user asked: "{userPrompt}"
            The tool {call.Name} returned: {result}
            Now answer normally without more tool calls.
            """;

			// Stream model response
			await foreach (var update in agent.RunStreamingAsync(followup, thread).WithCancellation(abort))
			{
				foreach (var c in update.Contents.OfType<TextContent>())
				{
					if (c.Text.Contains("<tool_call>"))
						continue;

					await WriteEvent(ctx, new
					{
						messageId,
						delta = c.Text,
						type = "TEXT_MESSAGE_CONTENT"
					}, abort);
				}

				await ctx.Response.Body.FlushAsync(abort);
			}
		}
		catch (Exception ex)
		{
			await WriteEvent(ctx, new
			{
				messageId,
				delta = $"Tool error: {ex.Message}",
				type = "TEXT_MESSAGE_CONTENT"
			}, abort);
		}
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

	private static string BuildAugmentedPrompt(List<AGUIMessage> messages, WorkspaceContext ctx)
	{

		if (ctx == null) {

			return string.Join("\n", messages.Select(m => $"<|{m.Role}|>\n{m.Content}"));
		}

		var sb = new StringBuilder();

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

