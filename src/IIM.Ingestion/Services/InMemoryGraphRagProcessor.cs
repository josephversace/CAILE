using System;
using System.Collections.Generic;
using System.Text;
using GraphRag.Config;
using GraphRag.Data;
using GraphRag.Indexing.Runtime;
using GraphRag.Storage;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.Extensions.DependencyInjection;
using GraphRagConfig = GraphRag.Config.GraphRagConfig;

namespace IIM.Ingestion.Services
{
    public class InMemoryGraphRagPipeline : IGraphRagPipeline
    {
        private readonly IServiceProvider _services;
        private readonly IPipelineFactory _pipelineFactory;
        private readonly PipelineExecutor _executor;

        public InMemoryGraphRagPipeline(IServiceProvider services)
        {
            _services = services;
            _pipelineFactory = services.GetRequiredService<IPipelineFactory>();
            _executor = services.GetRequiredService<PipelineExecutor>();
        }

        public async Task<GraphRagResult> ProcessAsync(
            IEnumerable<DocumentInput> documents,
            GraphRagConfig? config = null,
            CancellationToken ct = default)
        {
            var inputStorage = new MemoryPipelineStorage();
            var outputStorage = new MemoryPipelineStorage();

            // Load documents into memory storage
            foreach (var doc in documents)
            {
                var bytes = doc.Content switch
                {
                    byte[] b => b,
                    Stream s => await ReadStreamAsync(s),
                    string text => System.Text.Encoding.UTF8.GetBytes(text),
                    _ => throw new ArgumentException($"Unsupported content type for {doc.FileName}")
                };

                await inputStorage.SetAsync(doc.FileName, new MemoryStream(bytes), cancellationToken: ct);
            }

            config ??= CreateDefaultConfig();

            var pipeline = _pipelineFactory.BuildIndexingPipeline(IndexingPipelineDefinitions.Standard);
            var context = PipelineContextFactory.Create(
                inputStorage: inputStorage,
                outputStorage: outputStorage,
                services: _services
            );

            var errors = new List<Exception>();
            await foreach (var result in _executor.ExecuteAsync(pipeline, config, context, ct))
            {
                if (result.Errors is { Count: > 0 })
                {
                    errors.AddRange(result.Errors);
                }
            }

            return new GraphRagResult
            {
                Entities = await outputStorage.LoadTableAsync<GraphRag.Entities.EntityRecord>("entities", ct),
                Relationships = await outputStorage.LoadTableAsync<GraphRag.Relationships.RelationshipRecord>("relationships", ct),
                Communities = await outputStorage.LoadTableAsync<GraphRag.Community.CommunityRecord>("communities", ct),
                CommunityReports = await outputStorage.LoadTableAsync<GraphRag.Community.CommunityReportRecord>("community_reports", ct),
                TextUnits = await outputStorage.LoadTableAsync<TextUnitRecord>("text_units", ct),
                Documents = await outputStorage.LoadTableAsync<DocumentRecord>("documents", ct),
                Errors = errors
            };
        }

        private static async Task<byte[]> ReadStreamAsync(Stream stream)
        {
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            return ms.ToArray();
        }

        private static GraphRagConfig CreateDefaultConfig() => new()
        {
            Input = new InputConfig { FileType = InputFileType.Text, FilePattern = @".*\.txt$" },
            Chunks = new ChunkingConfig { Size = 1200, Overlap = 100 },
            ExtractGraph = new ExtractGraphConfig { ModelId = "chat_model" },
            ClusterGraph = new ClusterGraphConfig { Algorithm = CommunityDetectionAlgorithm.FastLabelPropagation },
            CommunityReports = new CommunityReportsConfig { ModelId = "chat_model" }
        };
    }

  
}
