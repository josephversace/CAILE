using System;
using System.Collections.Generic;
using System.Text;
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

		private IEmbeddingGenerator<string, Embedding<float>>? _generator;
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
			var templates = scope.ServiceProvider
				.GetRequiredService<IModelConfigurationTemplateService>();

			var template = await templates.GetDefaultTemplateAsync(ct);
			var embeddingCfg = template?.Models?.Embedding;

			if (embeddingCfg == null)
			{
				_logger.LogWarning("No embedding model configured for active tier.");
				return;
			}

			_logger.LogInformation(
				"Initializing embedding model {Id} ({Dims} dims)",
				embeddingCfg.Id,
				embeddingCfg.Dimensions);

			// IMPORTANT: this resolves YOUR OnnxEmbeddingGenerator
			_generator = scope.ServiceProvider
				.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

			VectorSize = embeddingCfg.Dimensions;
		}

		public async Task<IReadOnlyList<float[]>> EmbedAsync(
			IReadOnlyList<string> texts,
			CancellationToken ct = default)
		{
			if (!IsReady)
				throw new InvalidOperationException("Embedding service not initialized.");

			if (texts.Count == 0)
				return Array.Empty<float[]>();

			await _semaphore.WaitAsync(ct);
			try
			{
				var embeddings = await _generator!.GenerateAsync(
					texts,
					cancellationToken: ct);

				return embeddings
					.Select(e => e.Vector.ToArray())
					.ToList();
			}
			finally
			{
				_semaphore.Release();
			}
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
