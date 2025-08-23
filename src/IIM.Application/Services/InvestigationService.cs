using IIM.Application.Interfaces;
using IIM.Core.AI;
using IIM.Core.Configuration;
using IIM.Core.Models;
using IIM.Core.Services;

using IIM.Shared.DTOs;
using IIM.Shared.Enums;
using IIM.Shared.DTOs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Models;

namespace IIM.Application.Services
{
    public class InvestigationService : IInvestigationService
    {
        private readonly ILogger<InvestigationService> _logger;
        private readonly ISessionService _sessionService;
        private readonly IModelOrchestrator _modelOrchestrator;
        private readonly IModelConfigurationTemplateService _templateService;
        private readonly IExportService _exportService;
   
        private readonly IVisualizationService _visualizationService;

        private readonly Dictionary<string, SessionModelTracking> _sessionModelTracking = new();
        private readonly Dictionary<string, Case> _cases = new();
        private readonly Dictionary<string, InvestigationResponse> _responses = new();
        private readonly SemaphoreSlim _trackingLock = new(1, 1);

        public InvestigationService(
            ILogger<InvestigationService> logger,
            ISessionService sessionService,
            IModelOrchestrator modelOrchestrator,
            IModelConfigurationTemplateService templateService,
            IExportService exportService,
     
            IVisualizationService visualizationService)
        {
            _logger = logger;
            _sessionService = sessionService;
            _modelOrchestrator = modelOrchestrator;
            _templateService = templateService;
            _exportService = exportService;
   
            _visualizationService = visualizationService;

            InitializeSampleData();
        }

        #region Session Management

        public async Task<InvestigationSession> CreateSessionAsync(
            CreateSessionRequest request,
            CancellationToken cancellationToken = default)
        {
            var session = await _sessionService.CreateSessionAsync(request, cancellationToken);

            await _trackingLock.WaitAsync(cancellationToken);
            try
            {
                _sessionModelTracking[session.Id] = new SessionModelTracking
                {
                    SessionId = session.Id,
                    Models = new Dictionary<string, ModelConfiguration>()
                };
            }
            finally
            {
                _trackingLock.Release();
            }

            session.EnabledTools = GetDefaultTools();
            session.Models = GetDefaultModels();

            _logger.LogInformation("Created investigation session {SessionId} for case {CaseId}",
                session.Id, request.CaseId);

            return session;
        }

        public Task<InvestigationSession> GetSessionAsync(
            string sessionId,
            CancellationToken cancellationToken = default)
        {
            return _sessionService.GetSessionAsync(sessionId, cancellationToken);
        }

        public async Task<List<InvestigationSession>> GetSessionsByCaseAsync(
            string caseId,
            CancellationToken cancellationToken = default)
        {
            return await _sessionService.GetSessionsByCaseAsync(caseId, cancellationToken);
        }

        public Task<bool> DeleteSessionAsync(
            string sessionId,
            CancellationToken cancellationToken = default)
        {
            return _sessionService.CloseSessionAsync(sessionId, cancellationToken);
        }

        #endregion

        #region Query Processing

        public async Task<InvestigationResponse> ProcessQueryAsync(
            string sessionId,
            InvestigationQuery query,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Processing query for session {SessionId}", sessionId);

            var session = await _sessionService.GetSessionAsync(sessionId, cancellationToken);

            // Create user message
            var userMessage = new InvestigationMessage
            {
                Role = MessageRole.User,
                Content = query.Text,
                Attachments = query.Attachments,
                Timestamp = query.Timestamp
            };

            await _sessionService.AddMessageAsync(sessionId, userMessage, cancellationToken);

            // Create response with actual properties
            var response = new InvestigationResponse
            {
                Id = Guid.NewGuid().ToString(),
                Message = $"Processing: {query.Text}",
                ToolResults = new List<ToolResult>(),
                Citations = new List<Citation>(),
                RelatedEvidence = new List<Evidence>(),
                Metadata = new Dictionary<string, object>
                {
                    ["sessionId"] = sessionId,
                    ["queryTimestamp"] = query.Timestamp
                },
                Confidence = 0.95,
                DisplayType = ResponseDisplayType.Auto
            };

            // Store for later retrieval
            _responses[response.Id] = response;

            // Execute tools if requested
            if (query.EnabledTools?.Any() == true)
            {
                foreach (var tool in query.EnabledTools)
                {
                    var toolResult = await ExecuteToolAsync(
                        sessionId, tool, query.Context, cancellationToken);
                    response.ToolResults.Add(toolResult);
                }
            }

            // Add assistant response
            var assistantMessage = new InvestigationMessage
            {
                Role = MessageRole.Assistant,
                Content = response.Message,
                ToolResults = response.ToolResults,
                Citations = response.Citations,
                Timestamp = DateTimeOffset.UtcNow,
                ModelUsed = session.Models.Values.FirstOrDefault()?.ModelId
            };

            await _sessionService.AddMessageAsync(sessionId, assistantMessage, cancellationToken);

            return response;
        }

        public Task<InvestigationResponse> SendQueryAsync(
            string sessionId,
            InvestigationQuery query,
            CancellationToken cancellationToken = default)
        {
            return ProcessQueryAsync(sessionId, query, cancellationToken);
        }

        #endregion

        #region Tool Execution

        public async Task<ToolResult> ExecuteToolAsync(
            string sessionId,
            string toolName,
            Dictionary<string, object> parameters,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Executing tool {ToolName} for session {SessionId}", toolName, sessionId);

            var startTime = DateTimeOffset.UtcNow;

            var result = new ToolResult
            {
                Id = Guid.NewGuid().ToString("N"),
                ToolName = toolName,
                Status = ToolStatus.Success,
                Data = new { message = $"Executed {toolName}", parameters },
                ExecutedAt = startTime,
                ExecutionTime = TimeSpan.FromMilliseconds(Random.Shared.Next(100, 500)),
                Visualizations = new List<Visualization>(),
                Recommendations = new List<string>(),
                Metadata = new Dictionary<string, object>
                {
                    ["sessionId"] = sessionId,
                    ["executedBy"] = Environment.UserName
                }
            };

            return result;
        }

        #endregion

        #region Case Management

        public Task<List<Case>> GetRecentCasesAsync(int count = 10, CancellationToken cancellationToken = default)
        {
            var recentCases = _cases.Values
                .OrderByDescending(c => c.UpdatedAt)
                .Take(count)
                .ToList();

            return Task.FromResult(recentCases);
        }

        public Task<List<InvestigationSession>> GetCaseSessionsAsync(
            string caseId,
            CancellationToken cancellationToken = default)
        {
            return GetSessionsByCaseAsync(caseId, cancellationToken);
        }

        public Task<Case> GetCaseAsync(string caseId, CancellationToken cancellationToken = default)
        {
            if (_cases.TryGetValue(caseId, out var caseEntity))
            {
                return Task.FromResult(caseEntity);
            }

            throw new KeyNotFoundException($"Case {caseId} not found");
        }

        #endregion

        #region Response Management

        public Task<InvestigationResponse> EnrichResponseForDisplayAsync(
            InvestigationResponse response,
            InvestigationMessage? message = null)
        {
            response.DisplayMetadata = new Dictionary<string, object>
            {
                ["hasToolResults"] = response.ToolResults?.Any() ?? false,
                ["hasCitations"] = response.Citations?.Any() ?? false,
                ["hasEvidence"] = response.RelatedEvidence?.Any() ?? false,
                ["confidence"] = response.Confidence ?? 0
            };

            if (message != null)
            {
                response.DisplayMetadata["messageId"] = message.Id;
                response.DisplayMetadata["messageRole"] = message.Role.ToString();
            }

            return Task.FromResult(response);
        }

        public Task<InvestigationResponse> GetResponseAsync(string responseId)
        {
            if (_responses.TryGetValue(responseId, out var response))
            {
                return Task.FromResult(response);
            }

            return Task.FromResult(new InvestigationResponse
            {
                Id = responseId,
                Message = "Response not found",
                Metadata = new Dictionary<string, object> { ["notFound"] = true }
            });
        }

        public async Task<byte[]> ExportResponseAsync(
            string responseId,
            ExportFormat format,
            ExportOptions? options = null)
        {
            var response = await GetResponseAsync(responseId);

            // Use the IExportService for response export
            var exportResult = await _exportService.ExportResponseAsync(
                response,
                format,
                options ?? new ExportOptions());

            return exportResult.Data;
        }

        #endregion

        #region Model Management

        private async Task LoadModelAsync(ModelConfiguration config, CancellationToken cancellationToken)
        {
            // ModelRequest is a class with required properties
            var modelRequest = new ModelRequest
            {
                ModelId = config.ModelId,
                ModelPath = GetModelPath(config.ModelId),
                ModelType = DetermineModelType(config.ModelId),
                Provider = config.Provider
            };

            await _modelOrchestrator.LoadModelAsync(modelRequest, null, cancellationToken);
        }

        private string GetModelPath(string modelId)
        {
            var baseDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "IIM",
                "Models"
            );
            Directory.CreateDirectory(baseDir);

            var safeModelId = modelId.Replace(":", "_").Replace("/", "_");
            return Path.Combine(baseDir, safeModelId);
        }

        private ModelType DetermineModelType(string modelId)
        {
            var lower = modelId.ToLowerInvariant();

            if (lower.Contains("whisper")) return ModelType.Whisper;
            if (lower.Contains("clip") || lower.Contains("vision")) return ModelType.CLIP;
            if (lower.Contains("embed")) return ModelType.Embedding;

            return ModelType.LLM;
        }

        #endregion

        #region Private Helper Methods

        private void InitializeSampleData()
        {
            var sampleCase = new Case
            {
                Id = "case-001",
                Name = "Sample Investigation",
                Description = "Initial test case",
                Status = CaseStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-7),
                UpdatedAt = DateTimeOffset.UtcNow,
                Priority = CasePriority.High,
                LeadInvestigator = Environment.UserName,
                TeamMembers = new List<string> { Environment.UserName },
                Metadata = new Dictionary<string, object>
                {
                    ["category"] = "test",
                    ["version"] = "1.0"
                }
            };

            _cases[sampleCase.Id] = sampleCase;
        }

        private List<string> GetDefaultTools()
        {
            return new List<string> { "search", "analyze", "extract", "summarize" };
        }

        private Dictionary<string, ModelConfiguration> GetDefaultModels()
        {
            return new Dictionary<string, ModelConfiguration>
            {
                ["primary"] = new ModelConfiguration
                {
                    ModelId = "llama3.1:70b",
                    Provider = "Ollama",
                    Type = ModelType.LLM,
                    Status = ModelStatus.Available
                }
            };
        }

        #endregion
    }

    public class SessionModelTracking
    {
        public string SessionId { get; set; } = string.Empty;
        public Dictionary<string, ModelConfiguration> Models { get; set; } = new();
    }
}