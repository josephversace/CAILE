using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

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
		IToolRegistry tools)
	{
		await RunAgentAsync(ctx, agentFactory.GetChatAgentAsync, tools);
	}

	private static async Task HandleReasoningAsync(
		HttpContext ctx,
		IAIAgentFactory agentFactory,
		IToolRegistry tools)
	{
		await RunAgentAsync(ctx, agentFactory.GetReasoningAgentAsync, tools);
	}

	// ============================================================
	// CORE EXECUTION PIPELINE (Handles streaming + tools + errors)
	// ============================================================
	private static async Task RunAgentAsync(
		HttpContext ctx,
		Func<Task<AIAgent>> agentResolver,
		IToolRegistry toolRegistry)
	{
		// SSE headers
		ctx.Response.Headers["Content-Type"] = "text/event-stream";
		ctx.Response.Headers["Cache-Control"] = "no-cache, no-transform";
		ctx.Response.Headers["Connection"] = "keep-alive";
		ctx.Response.Headers["X-Accel-Buffering"] = "no";

		var abort = ctx.RequestAborted;

		// Parse request
		var req = await JsonSerializer.DeserializeAsync<AGUIRequest>(
			ctx.Request.Body, cancellationToken: abort);

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

		// Build full conversation context
		string prompt = string.Join("\n", req.Messages.Select(m =>
			$"<|{m.Role}|>\n{m.Content}"));


		
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

		try
		{
			await foreach (var update in agent.RunStreamingAsync(prompt, thread).WithCancellation(abort))
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
										messageId, prompt, abort
									);
								}
							}
						}
					}
				}

				await ctx.Response.Body.FlushAsync(abort);
			}
		}
		catch (OperationCanceledException)
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
			result = (object?)null
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

	// ============================================================
	// SSE Writer
	// ============================================================
	private static async Task WriteEvent(HttpContext ctx, object data, CancellationToken ct)
	{
		try
		{
			// Stop immediately if client disconnected
			ct.ThrowIfCancellationRequested();

			var json = JsonSerializer.Serialize(data);

			await ctx.Response.WriteAsync($"event: message\ndata: {json}\n\n");
			await ctx.Response.Body.FlushAsync();
		}
		catch (OperationCanceledException)
		{
			// Client disconnected — safe to ignore
		}
		catch (IOException)
		{
			// Response pipe closed — safe to ignore
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
}

// ======================================================================
// AG-UI DTOs (Stable)
// ======================================================================
public class AGUIRequest
{
	[JsonPropertyName("threadId")] public string ThreadId { get; set; } = "";
	[JsonPropertyName("runId")] public string RunId { get; set; } = "";
	[JsonPropertyName("messages")] public List<AGUIMessage> Messages { get; set; } = new();
	[JsonPropertyName("context")] public List<object> Context { get; set; } = new();
}

public class AGUIMessage
{
	[JsonPropertyName("id")] public string Id { get; set; } = "";
	[JsonPropertyName("role")] public string Role { get; set; } = "";
	[JsonPropertyName("content")] public string Content { get; set; } = "";
	[JsonPropertyName("name")] public string? Name { get; set; }
}
