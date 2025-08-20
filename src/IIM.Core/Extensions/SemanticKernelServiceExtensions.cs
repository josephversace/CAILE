// src/IIM.Core/Extensions/SemanticKernelServiceExtensions.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using IIM.Core.AI;
using IIM.Core.Models;
using IIM.Core.Services;
using IIM.Core.Services.Configuration;
using IIM.Infrastructure.Storage;
using IIM.Shared.Enums;
using IIM.Shared.Models;

namespace IIM.Core.Extensions
{
    /// <summary>
    /// Extension methods for registering Semantic Kernel services
    /// </summary>
    public static class SemanticKernelServiceExtensions
    {
        /// <summary>
        /// Adds Semantic Kernel orchestration to the service collection
        /// </summary>
        public static IServiceCollection AddSemanticKernelOrchestration(
            this IServiceCollection services)
        {
            // Register the template service if not already registered
            services.AddSingleton<IModelConfigurationTemplateService>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<ModelConfigurationTemplateService>>();
                var storageConfig = sp.GetRequiredService<StorageConfiguration>();

                // Note: These dependencies might need to be mocked or stubbed initially
                var sessionService = sp.GetService<ISessionService>()
                    ?? new MockSessionService(sp.GetRequiredService<ILogger<MockSessionService>>());

                // Use the existing orchestrator (will be replaced by SK orchestrator)
                var modelOrchestrator = sp.GetService<IModelOrchestrator>()
                    ?? new SemanticKernelOrchestrator(
                        sp.GetRequiredService<ILogger<SemanticKernelOrchestrator>>(),
                        null); // Avoid circular dependency

                return new ModelConfigurationTemplateService(
                    logger,
                    storageConfig,
                    modelOrchestrator,
                    sessionService);
            });

            // Register the SK Orchestrator as the primary IModelOrchestrator
            services.AddSingleton<IModelOrchestrator>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<SemanticKernelOrchestrator>>();
                var templateService = sp.GetService<IModelConfigurationTemplateService>();

                return new SemanticKernelOrchestrator(logger, templateService);
            });

            // Register Kernel builder factory
            services.AddSingleton<IKernelBuilder>(sp => Kernel.CreateBuilder());

            // Register default kernel
            services.AddTransient<Kernel>(sp =>
            {
                var builder = sp.GetRequiredService<IKernelBuilder>();
                return builder.Build();
            });

            return services;
        }

        /// <summary>
        /// Adds investigation plugins to Semantic Kernel
        /// </summary>
        public static IServiceCollection AddInvestigationPlugins(
            this IServiceCollection services)
        {
            // Register plugins as singletons
            services.AddSingleton<ForensicAnalysisPlugin>();
            // Note: OSINTPlugin and RAGPlugin need to be created

            // Register plugin with kernel on creation
            services.Configure<KernelBuilderOptions>(options =>
            {
                options.ConfigurePlugins = (kernel, sp) =>
                {
                    var forensics = sp.GetRequiredService<ForensicAnalysisPlugin>();
                    kernel.Plugins.AddFromObject(forensics, "ForensicAnalysis");

                    // Add other plugins when they're created
                    // var osint = sp.GetRequiredService<OSINTPlugin>();
                    // kernel.Plugins.AddFromObject(osint, "OSINT");
                };
            });

            return services;
        }
    }

    // Temporary mock implementation
    internal class MockSessionService : ISessionService
    {
        private readonly ILogger<MockSessionService> _logger;
        private readonly Dictionary<string, InvestigationSession> _sessions = new();

        public MockSessionService(ILogger<MockSessionService> logger)
        {
            _logger = logger;
        }

        public Task<InvestigationSession> GetSessionAsync(string id, CancellationToken ct)
        {
            if (_sessions.TryGetValue(id, out var session))
            {
                return Task.FromResult(session);
            }

            _logger.LogWarning("Using mock session service - returning default session");
            return Task.FromResult(new InvestigationSession
            {
                Id = id,
                CaseId = "mock-case",
                CreatedBy = "system",
                Models = new Dictionary<string, ModelConfiguration>(),
                EnabledTools = new List<string>()
            });
        }

        public Task<InvestigationSession> CreateSessionAsync(CreateSessionRequest request, CancellationToken ct)
        {
            var session = new InvestigationSession
            {
                Id = Guid.NewGuid().ToString(),
                CaseId = request.CaseId,
                CreatedBy = "system",
                CreatedAt = DateTimeOffset.UtcNow,
                Status = InvestigationStatus.Active,
                Models = new Dictionary<string, ModelConfiguration>(),
                EnabledTools = new List<string>()
            };

            _sessions[session.Id] = session;
            return Task.FromResult(session);
        }

        public Task<InvestigationSession> UpdateSessionAsync(string id, Action<InvestigationSession> updateAction, CancellationToken ct)
        {
            if (_sessions.TryGetValue(id, out var session))
            {
                updateAction(session);
                return Task.FromResult(session);
            }
            throw new KeyNotFoundException($"Session {id} not found");
        }

        public Task<bool> CloseSessionAsync(string id, CancellationToken ct)
        {
            if (_sessions.TryGetValue(id, out var session))
            {
                session.Status = InvestigationStatus.Completed;
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public Task<List<InvestigationSession>> GetAllSessionsAsync(CancellationToken ct)
        {
            return Task.FromResult(_sessions.Values.ToList());
        }

        public Task<List<InvestigationSession>> GetSessionsByCaseAsync(string caseId, CancellationToken ct)
        {
            var sessions = _sessions.Values.Where(s => s.CaseId == caseId).ToList();
            return Task.FromResult(sessions);
        }

        public Task<bool> DeleteSessionAsync(string id, CancellationToken ct)
        {
            return Task.FromResult(_sessions.Remove(id));
        }

        public Task<InvestigationSession> AddMessageAsync(string sessionId, InvestigationMessage message, CancellationToken ct)
        {
            if (_sessions.TryGetValue(sessionId, out var session))
            {
                session.Messages.Add(message);
                return Task.FromResult(session);
            }
            throw new KeyNotFoundException($"Session {sessionId} not found");
        }
    }

    // Configuration options
    public class KernelBuilderOptions
    {
        public Action<Kernel, IServiceProvider>? ConfigurePlugins { get; set; }
    }
}