using IIM.Shared.Enums;
using IIM.Shared.Interfaces;
using LLama;
using LLama.Abstractions;
using LLama.Common;
using LLama.Native;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Infrastructure.AI.LlamaSharp
{
    /// <summary>
    /// Thin runtime manager for GGUF/GGML models using LLamaSharp.
    /// Handles only inference, session/context, and basic preprocessing.
    /// </summary>

    public class LlamaSharpManager : ILlamaSharpManager
    {
        private readonly ILogger<LlamaSharpManager> _logger;
        private readonly ConcurrentDictionary<string, LLamaWeights> _weightsCache = new();
        private readonly ConcurrentDictionary<string, LLamaContext> _contextCache = new();
        private readonly SemaphoreSlim _contextLock = new(1, 1);

        public LlamaSharpManager(ILogger<LlamaSharpManager> logger)
        {
            _logger = logger;
        }

        public async Task<LLamaContext> CreateContextAsync(string modelPath, ModelParams? parameters = null)
        {
            var modelParams = parameters ?? new ModelParams(modelPath);
            var cacheKey = modelParams.ModelPath;

            await _contextLock.WaitAsync();
            try
            {
                if (_contextCache.TryGetValue(cacheKey, out var context))
                {
                    _logger.LogDebug("Using cached LLamaContext for {Model}", modelPath);
                    return context;
                }

                _logger.LogInformation("Loading LLamaWeights for {Model}", modelPath);
                var weights = LLamaWeights.LoadFromFile(modelParams);
                _weightsCache[cacheKey] = weights;

                _logger.LogInformation("Creating LLamaContext for {Model}", modelPath);
                context = weights.CreateContext(modelParams);
                _contextCache[cacheKey] = context;

                return context;
            }
            finally
            {
                _contextLock.Release();
            }
        }

        // For a prompt (not chat)
        public async Task<string> RunPromptAsync(
            LLamaContext context,
            string prompt,
            InferenceParams? genParams = null,
            CancellationToken cancellationToken = default)
        {
            var executor = new InteractiveExecutor(context);
            _logger.LogDebug("Running LLamaSharp prompt inference");

            var result = "";
            await foreach (var token in executor.InferAsync(prompt, genParams ?? new InferenceParams())
                .WithCancellation(cancellationToken))
            {
                result += token;
            }
            return result;
        }

        // For chat, maintaining a chat session
        public async IAsyncEnumerable<string> RunChatAsync(ChatSession session, string userPrompt, InferenceParams? genParams = null)
        {
            await foreach (var token in session.ChatAsync(
                new ChatHistory.Message(AuthorRole.User, userPrompt),
                genParams ?? new InferenceParams()))
            {
                yield return token;
            }
        }

        // Optionally: create a ChatSession with a loaded context and chat history
        public ChatSession CreateChatSession(LLamaContext context, ChatHistory? history = null)
        {
            var executor = new InteractiveExecutor(context);
            return new ChatSession(executor, history ?? new ChatHistory());
        }
    }


}
