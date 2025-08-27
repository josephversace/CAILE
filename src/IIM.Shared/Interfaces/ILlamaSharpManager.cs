using LLama;
using LLama.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces;


public interface ILlamaSharpManager
{
    ChatSession CreateChatSession(LLamaContext context, ChatHistory? history = null);
    Task<LLamaContext> CreateContextAsync(string modelPath, ModelParams? parameters = null);
    IAsyncEnumerable<string> RunChatAsync(ChatSession session, string userPrompt, InferenceParams? genParams = null);
    Task<string> RunPromptAsync(LLamaContext context, string prompt, InferenceParams? genParams = null, CancellationToken cancellationToken = default);
}

