using System;
using System.Collections.Generic;
using System.Text;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IIM.Infrastructure.Services
{
	public sealed class EmbeddingService : IEmbeddingService, IDisposable
	{
		private readonly ILogger<EmbeddingService> _logger;
		private readonly IServiceScopeFactory _scopeFactory;
		private readonly SemaphoreSlim _semaphore = new(1, 1);

		private IEmbeddingGenerator<EmbeddingWorkItem, Embedding<float>> _generator;

		private bool _disposed;

		public bool IsReady => _generator != null && !_disposed;
		public int VectorSize { get; private set; }

		public EmbeddingService(
			IServiceScopeFactory scopeFactory,
			ILogger<EmbeddingService> logger)
		{
			_scopeFactory = scopeFactory;
			_logger = logger;
		}

		public async Task InitializeAsync(CancellationToken ct = default)
		{
			if (IsReady)
				return;

			using var scope = _scopeFactory.CreateScope();
			var config = scope.ServiceProvider
				.GetRequiredService<IModelResolver>();

		
			var embeddingCfg = await config.GetEmbeddingModelAsync();

			if (embeddingCfg == null)
			{
				_logger.LogWarning("No embedding model configured.");
				return;
			}

			_logger.LogInformation(
				"Initializing embedding model {Id} ({Dims} dims)",
				embeddingCfg.ModelId,
				embeddingCfg.Dimensions);

			// IMPORTANT: this resolves YOUR OnnxEmbeddingGenerator
			_generator = scope.ServiceProvider
				.GetRequiredService<IEmbeddingGenerator<EmbeddingWorkItem, Embedding<float>>>();

			VectorSize = embeddingCfg.Dimensions;
		}

		public async Task<IReadOnlyList<float[]>> EmbedAsync(
			IReadOnlyList<EmbeddingWorkItem> items,
			CancellationToken ct = default)
		{
			if (!IsReady)
				throw new InvalidOperationException("Embedding service not initialized.");

			if (items.Count == 0)
				return Array.Empty<float[]>();

			await _semaphore.WaitAsync(ct);
			try
			{
				// Defensive enforcement: memory safety only
				var safeItems = new List<EmbeddingWorkItem>(items.Count);

				foreach (var item in items)
				{
					safeItems.Add(item with
					{
						Text = item.Text
					});
				}

				var options = new EmbeddingGenerationOptions
				{
					// Optional: pin model or dimensions if you want
					// ModelId = "your-embedding-model-id",
					// Dimensions = 384
				};

				foreach (var item in safeItems)
				{
					if (item.Text.Length > 50_000)
					{
						throw new InvalidOperationException(
							$"EmbeddingWorkItem too large: {item.Text.Length} chars");
					}
				}

				var embeddings = await _generator.GenerateAsync(
					safeItems,
					options,
					ct);

				return embeddings
					.Select(e => e.Vector.ToArray())
					.ToList();
			}
			finally
			{
				_semaphore.Release();
			}
		}

		// Character-based hard safety limit to protect tokenizer memory
		private const int MaxCharsPerChunk = 4000;

		private static string TruncateByChars(string text)
		{
			if (string.IsNullOrEmpty(text))
				return string.Empty;

			if (text.Length <= MaxCharsPerChunk)
				return text;

			return text.Substring(0, MaxCharsPerChunk);
		}

		public void Dispose()
		{
			if (_disposed) return;
			_disposed = true;
			_semaphore.Dispose();

			if (_generator is IDisposable d)
				d.Dispose();
		}
	}
}