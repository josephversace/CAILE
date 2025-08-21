using IIM.Core.Models;
using IIM.Core.Services;
using IIM.Core.Services.Configuration;
using IIM.Shared.DTOs;
using IIM.Shared.Enums;
using IIM.Shared.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.TextGeneration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Core.AI
{
    /// <summary>
    /// Semantic Kernel-based implementation of IReasoningService.
    /// This is the intelligence layer responsible for WHAT to do - parsing intent,
    /// chaining operations, and managing complex multi-step reasoning.
    /// It delegates model loading/management to IModelOrchestrator.
    /// </summary>
    public class SemanticKernelOrchestrator : IReasoningService
    {
        private readonly ILogger<SemanticKernelOrchestrator> _logger;
        private readonly IModelOrchestrator _modelOrchestrator;
        private readonly ISessionService _sessionService;
        private readonly IModelConfigurationTemplateService? _templateService;

        private readonly Dictionary<string, Kernel> _kernels = new();
        private readonly Dictionary<string, PluginInfo> _availablePlugins = new();
        private readonly SemaphoreSlim _kernelLock = new(1, 1);

        // Store session metadata separately since InvestigationSession doesn't have this property
        private readonly Dictionary<string, Dictionary<string, object>> _sessionMetadata = new();

        // Events
        public event EventHandler<ReasoningStartedEventArgs>? ReasoningStarted;
        public event EventHandler<ReasoningCompletedEventArgs>? ReasoningCompleted;
        public event EventHandler<ReasoningStepCompletedEventArgs>? StepCompleted;
        public event EventHandler<ReasoningErrorEventArgs>? ReasoningError;

        /// <summary>
        /// Initializes the Semantic Kernel orchestrator with required dependencies.
        /// </summary>
        public SemanticKernelOrchestrator(
            ILogger<SemanticKernelOrchestrator> logger,
            IModelOrchestrator modelOrchestrator,
            ISessionService sessionService,
            IModelConfigurationTemplateService? templateService = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
            _templateService = templateService;

            InitializeBuiltInPlugins();
        }

        #region Core Reasoning Operations

        /// <summary>
        /// Processes a natural language query and determines the appropriate action plan.
        /// </summary>
        public async Task<ReasoningResult> ProcessQueryAsync(
            string query,
            InvestigationSession? session = null,
            CancellationToken cancellationToken = default)
        {
            var queryId = Guid.NewGuid().ToString();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("Processing query {QueryId}: {Query}", queryId, query);

                // Raise reasoning started event
                ReasoningStarted?.Invoke(this, new ReasoningStartedEventArgs
                {
                    QueryId = queryId,
                    Query = query,
                    StartTime = DateTime.UtcNow
                });

                // Extract intent from the query
                var intent = await ExtractIntentAsync(query, cancellationToken);

                // Build action plan based on intent
                var actionPlan = await BuildActionPlanAsync(intent, session, cancellationToken);

                // Extract entities from the query
                var entities = await ExtractEntitiesAsync(query, cancellationToken);

                var result = new ReasoningResult
                {
                    QueryId = queryId,
                    OriginalQuery = query,
                    Intent = intent,
                    ActionPlan = actionPlan,
                    ExtractedEntities = entities,
                    Confidence = intent.Confidence,
                    ProcessingTime = stopwatch.Elapsed
                };

                // Raise reasoning completed event
                ReasoningCompleted?.Invoke(this, new ReasoningCompletedEventArgs
                {
                    QueryId = queryId,
                    Result = result,
                    Duration = stopwatch.Elapsed
                });

                _logger.LogInformation(
                    "Query {QueryId} processed successfully in {Time}ms with confidence {Confidence}",
                    queryId, stopwatch.ElapsedMilliseconds, intent.Confidence);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing query {QueryId}", queryId);

                ReasoningError?.Invoke(this, new ReasoningErrorEventArgs
                {
                    OperationId = queryId,
                    Error = ex.Message,
                    Exception = ex
                });

                throw;
            }
        }

        /// <summary>
        /// Executes a multi-step reasoning chain for complex operations.
        /// </summary>
        public async Task<ChainExecutionResult> ExecuteReasoningChainAsync(
            ReasoningChain chain,
            IProgress<ReasoningProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Executing reasoning chain {ChainId}: {Name}", chain.ChainId, chain.Name);

            var stopwatch = Stopwatch.StartNew();
            var stepResults = new List<StepResult>();
            var finalOutput = new Dictionary<string, object>();

            try
            {
                var totalSteps = chain.Steps.Count;
                var stepsCompleted = 0;

                // Execute based on mode
                switch (chain.Mode)
                {
                    case ChainExecutionMode.Sequential:
                        foreach (var step in chain.Steps)
                        {
                            var stepResult = await ExecuteStepAsync(step, chain.Context, cancellationToken);
                            stepResults.Add(stepResult);

                            stepsCompleted++;
                            progress?.Report(new ReasoningProgress
                            {
                                OperationId = chain.ChainId,
                                CurrentStep = step.Name,
                                StepsCompleted = stepsCompleted,
                                TotalSteps = totalSteps,
                                Status = stepResult.Success ? "Completed" : "Failed"
                            });

                            StepCompleted?.Invoke(this, new ReasoningStepCompletedEventArgs
                            {
                                ChainId = chain.ChainId,
                                StepId = step.StepId,
                                Result = stepResult
                            });

                            if (!stepResult.Success && !step.IsOptional)
                            {
                                throw new InvalidOperationException(
                                    $"Required step {step.Name} failed: {stepResult.ErrorMessage}");
                            }

                            // Pass output to context for next steps
                            if (stepResult.Output != null)
                            {
                                chain.Context[$"step_{step.StepId}_output"] = stepResult.Output;
                            }
                        }
                        break;

                    case ChainExecutionMode.Parallel:
                        var parallelTasks = chain.Steps.Select(step =>
                            ExecuteStepAsync(step, chain.Context, cancellationToken));

                        var parallelResults = await Task.WhenAll(parallelTasks);
                        stepResults.AddRange(parallelResults);

                        foreach (var result in parallelResults)
                        {
                            stepsCompleted++;
                            progress?.Report(new ReasoningProgress
                            {
                                OperationId = chain.ChainId,
                                CurrentStep = "Parallel execution",
                                StepsCompleted = stepsCompleted,
                                TotalSteps = totalSteps,
                                Status = "Completed"
                            });
                        }
                        break;

                    case ChainExecutionMode.Conditional:
                        // Execute steps based on conditions
                        foreach (var step in chain.Steps)
                        {
                            if (await ShouldExecuteStepAsync(step, chain.Context, cancellationToken))
                            {
                                var stepResult = await ExecuteStepAsync(step, chain.Context, cancellationToken);
                                stepResults.Add(stepResult);

                                if (stepResult.Output != null)
                                {
                                    chain.Context[$"step_{step.StepId}_output"] = stepResult.Output;
                                }
                            }

                            stepsCompleted++;
                            progress?.Report(new ReasoningProgress
                            {
                                OperationId = chain.ChainId,
                                CurrentStep = step.Name,
                                StepsCompleted = stepsCompleted,
                                TotalSteps = totalSteps,
                                Status = "Evaluating"
                            });
                        }
                        break;

                    case ChainExecutionMode.Adaptive:
                        // Dynamically adjust execution based on results
                        await ExecuteAdaptiveChainAsync(chain, stepResults, progress, cancellationToken);
                        break;
                }

                // Compile final output
                finalOutput = CompileFinalOutput(stepResults, chain.Context);

                return new ChainExecutionResult
                {
                    ChainId = chain.ChainId,
                    Success = stepResults.All(r => r.Success || chain.Steps.First(s => s.StepId == r.StepId).IsOptional),
                    StepResults = stepResults,
                    FinalOutput = finalOutput,
                    TotalExecutionTime = stopwatch.Elapsed
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing reasoning chain {ChainId}", chain.ChainId);

                return new ChainExecutionResult
                {
                    ChainId = chain.ChainId,
                    Success = false,
                    StepResults = stepResults,
                    FinalOutput = finalOutput,
                    TotalExecutionTime = stopwatch.Elapsed,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Analyzes evidence and generates investigative insights.
        /// </summary>
        public async Task<AnalysisResult> AnalyzeEvidenceAsync(
            List<string> evidenceIds,
            AnalysisType analysisType,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "Analyzing {Count} evidence items with {Type} analysis",
                evidenceIds.Count, analysisType);

            var stopwatch = Stopwatch.StartNew();
            var findings = new List<Finding>();
            var recommendations = new List<string>();

            try
            {
                // Get appropriate kernel for analysis
                var kernel = await GetKernelAsync($"analysis_{analysisType}", cancellationToken);

                // Perform analysis based on type
                switch (analysisType)
                {
                    case AnalysisType.Forensic:
                        findings = await PerformForensicAnalysisAsync(evidenceIds, kernel, cancellationToken);
                        break;

                    case AnalysisType.Temporal:
                        findings = await PerformTemporalAnalysisAsync(evidenceIds, kernel, cancellationToken);
                        break;

                    case AnalysisType.Relational:
                        findings = await PerformRelationalAnalysisAsync(evidenceIds, kernel, cancellationToken);
                        break;

                    case AnalysisType.Pattern:
                        findings = await PerformPatternAnalysisAsync(evidenceIds, kernel, cancellationToken);
                        break;

                    case AnalysisType.Anomaly:
                        findings = await PerformAnomalyDetectionAsync(evidenceIds, kernel, cancellationToken);
                        break;

                    case AnalysisType.Comprehensive:
                        // Run all analysis types
                        var tasks = new[]
                        {
                            PerformForensicAnalysisAsync(evidenceIds, kernel, cancellationToken),
                            PerformTemporalAnalysisAsync(evidenceIds, kernel, cancellationToken),
                            PerformRelationalAnalysisAsync(evidenceIds, kernel, cancellationToken),
                            PerformPatternAnalysisAsync(evidenceIds, kernel, cancellationToken)
                        };
                        var allFindings = await Task.WhenAll(tasks);
                        findings = allFindings.SelectMany(f => f).ToList();
                        break;
                }

                // Generate recommendations based on findings
                recommendations = GenerateRecommendations(findings, analysisType);

                return new AnalysisResult
                {
                    Type = analysisType,
                    Findings = findings,
                    Recommendations = recommendations,
                    Metadata = new Dictionary<string, object>
                    {
                        ["evidenceCount"] = evidenceIds.Count,
                        ["findingsCount"] = findings.Count,
                        ["analysisType"] = analysisType.ToString()
                    },
                    AnalysisTime = stopwatch.Elapsed,
                    ConfidenceScore = CalculateConfidenceScore(findings)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing evidence");
                throw;
            }
        }

        #endregion

        #region Semantic Kernel Management

        /// <summary>
        /// Gets or creates a Semantic Kernel configured for a specific purpose.
        /// </summary>
        public async Task<Kernel> GetKernelAsync(
            string purpose,
            CancellationToken cancellationToken = default)
        {
            await _kernelLock.WaitAsync(cancellationToken);
            try
            {
                // Return existing kernel if available
                if (_kernels.TryGetValue(purpose, out var existingKernel))
                {
                    _logger.LogDebug("Returning existing kernel for purpose: {Purpose}", purpose);
                    return existingKernel;
                }

                _logger.LogInformation("Creating new kernel for purpose: {Purpose}", purpose);

                // Create kernel builder
                var builder = Kernel.CreateBuilder();

                // Configure based on purpose
                await ConfigureKernelForPurposeAsync(builder, purpose, cancellationToken);

                // Build and store kernel
                var kernel = builder.Build();
                _kernels[purpose] = kernel;

                return kernel;
            }
            finally
            {
                _kernelLock.Release();
            }
        }

        /// <summary>
        /// Loads a plugin into the reasoning service.
        /// </summary>
        public async Task<bool> LoadPluginAsync(
            string pluginName,
            Dictionary<string, object>? configuration = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Loading plugin: {PluginName}", pluginName);

                // Get or create default kernel
                var kernel = await GetKernelAsync("default", cancellationToken);

                // Load plugin based on name
                switch (pluginName.ToLowerInvariant())
                {
                    case "forensics":
                    case "forensicanalysis":
                        // Load forensic analysis plugin
                        kernel.Plugins.AddFromType<ForensicAnalysisPlugin>(pluginName);
                        break;

                    case "osint":
                        // Load OSINT plugin when available
                        _logger.LogWarning("OSINT plugin not yet implemented");
                        return false;

                    case "rag":
                        // Load RAG plugin when available
                        _logger.LogWarning("RAG plugin not yet implemented");
                        return false;

                    default:
                        _logger.LogWarning("Unknown plugin: {PluginName}", pluginName);
                        return false;
                }

                // Update available plugins
                _availablePlugins[pluginName] = new PluginInfo
                {
                    Name = pluginName,
                    Description = $"{pluginName} plugin",
                    Version = "1.0.0",
                    IsLoaded = true,
                    Functions = GetPluginFunctions(kernel, pluginName),
                    Metadata = configuration ?? new Dictionary<string, object>()
                };

                _logger.LogInformation("Successfully loaded plugin: {PluginName}", pluginName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading plugin {PluginName}", pluginName);
                return false;
            }
        }

        /// <summary>
        /// Gets available plugins for the reasoning service.
        /// </summary>
        public Task<List<PluginInfo>> GetAvailablePluginsAsync(
            CancellationToken cancellationToken = default)
        {
            var plugins = new List<PluginInfo>(_availablePlugins.Values);

            // Add unloaded but available plugins
            var availableButNotLoaded = new[]
            {
                new PluginInfo
                {
                    Name = "OSINT",
                    Description = "Open Source Intelligence gathering",
                    Version = "1.0.0",
                    IsLoaded = false,
                    Functions = new List<string> { "SearchPublicRecords", "AnalyzeSocialMedia", "GatherIntelligence" }
                },
                new PluginInfo
                {
                    Name = "RAG",
                    Description = "Retrieval Augmented Generation",
                    Version = "1.0.0",
                    IsLoaded = false,
                    Functions = new List<string> { "SearchDocuments", "RetrieveContext", "GenerateAnswer" }
                }
            };

            plugins.AddRange(availableButNotLoaded.Where(p => !_availablePlugins.ContainsKey(p.Name)));

            return Task.FromResult(plugins);
        }

        #endregion

        #region Intent and Context Management

        /// <summary>
        /// Extracts intent from a user query.
        /// </summary>
        public async Task<IntentExtractionResult> ExtractIntentAsync(
            string query,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Extracting intent from query: {Query}", query);

            // For now, use simple keyword matching
            // In production, this would use the LLM for intent classification
            var intent = new IntentExtractionResult
            {
                PrimaryIntent = DetermineIntent(query),
                IntentScores = CalculateIntentScores(query),
                Entities = ExtractBasicEntities(query),
                Parameters = new Dictionary<string, object>(),
                Confidence = 0.85f
            };

            // If we have a model loaded, use it for better intent extraction
            var loadedModels = await _modelOrchestrator.GetLoadedModelsAsync(cancellationToken);
            if (loadedModels.Any(m => m.Type == ModelType.LLM))
            {
                // TODO: Use LLM for intent extraction
                _logger.LogDebug("Would use LLM for intent extraction if inference was available");
            }

            return intent;
        }

        /// <summary>
        /// Builds context from current investigation session.
        /// Adds supplementary data to the session for reasoning purposes.
        /// </summary>
        /// <param name="sessionId">Current session ID</param>
        /// <param name="includeHistory">Whether to include conversation history</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Built session with enriched context</returns>
        public async Task<InvestigationSession> BuildSessionContextAsync(
            string sessionId,
            bool includeHistory = true,
            CancellationToken cancellationToken = default)
        {
            var session = await _sessionService.GetSessionAsync(sessionId, cancellationToken);

            if (includeHistory && session.Messages.Any())
            {
                // Store metadata separately for this session since InvestigationSession doesn't have Metadata property
                if (!_sessionMetadata.ContainsKey(sessionId))
                {
                    _sessionMetadata[sessionId] = new Dictionary<string, object>();
                }

                _sessionMetadata[sessionId]["conversationSummary"] = SummarizeConversation(session.Messages);
                _sessionMetadata[sessionId]["messageCount"] = session.Messages.Count;
                _sessionMetadata[sessionId]["lastActivity"] = session.UpdatedAt;

                _logger.LogDebug("Session {SessionId} context built with {Count} messages", sessionId, session.Messages.Count);
            }

            return session;
        }

        /// <summary>
        /// Updates the session context with new information.
        /// Since InvestigationSession doesn't have Metadata, we store it separately.
        /// </summary>
        public async Task<InvestigationSession> UpdateSessionContextAsync(
            InvestigationSession session,
            Dictionary<string, object> updates,
            CancellationToken cancellationToken = default)
        {
            // Store metadata separately for this session
            if (!_sessionMetadata.ContainsKey(session.Id))
            {
                _sessionMetadata[session.Id] = new Dictionary<string, object>();
            }

            foreach (var update in updates)
            {
                _sessionMetadata[session.Id][update.Key] = update.Value;
            }

            // Update session timestamp
            session.UpdatedAt = DateTimeOffset.UtcNow;

            // Update session via service
            await _sessionService.UpdateSessionAsync(
                session.Id,
                s =>
                {
                    s.UpdatedAt = session.UpdatedAt;
                    // Add any other updates that the session supports
                },
                cancellationToken);

            return session;
        }

        /// <summary>
        /// Gets metadata for a session.
        /// </summary>
        private Dictionary<string, object> GetSessionMetadata(string sessionId)
        {
            return _sessionMetadata.TryGetValue(sessionId, out var metadata)
                ? metadata
                : new Dictionary<string, object>();
        }

        #endregion

        #region Prompt and Template Management

        /// <summary>
        /// Generates a prompt for a specific task.
        /// </summary>
        public Task<string> GeneratePromptAsync(
            string taskType,
            Dictionary<string, object> parameters,
            CancellationToken cancellationToken = default)
        {
            var prompt = taskType.ToLowerInvariant() switch
            {
                "analyze" => GenerateAnalysisPrompt(parameters),
                "summarize" => GenerateSummaryPrompt(parameters),
                "extract" => GenerateExtractionPrompt(parameters),
                "compare" => GenerateComparisonPrompt(parameters),
                "investigate" => GenerateInvestigationPrompt(parameters),
                _ => GenerateDefaultPrompt(taskType, parameters)
            };

            return Task.FromResult(prompt);
        }

        /// <summary>
        /// Applies a template to format output.
        /// </summary>
        public async Task<string> ApplyTemplateAsync(
            string templateName,
            object data,
            CancellationToken cancellationToken = default)
        {
            // If template service is available, use it
            if (_templateService != null)
            {
                var templates = await _templateService.GetTemplatesAsync(null, cancellationToken);
                var template = templates.FirstOrDefault(t => t.Name.Equals(templateName, StringComparison.OrdinalIgnoreCase));

                if (template != null)
                {
                    return FormatWithTemplate(template, data);
                }
            }

            // Otherwise use basic formatting
            return FormatBasic(templateName, data);
        }

        #endregion

        #region Model Routing and Selection

        /// <summary>
        /// Determines the best model for a given task.
        /// </summary>
        public async Task<ModelRecommendation> RecommendModelAsync(
            string task,
            ModelConstraints? constraints = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Recommending model for task: {Task}", task);

            var availableModels = await _modelOrchestrator.GetAvailableModelsAsync(cancellationToken);
            var loadedModels = await _modelOrchestrator.GetLoadedModelsAsync(cancellationToken);

            // Prefer loaded models
            var candidates = loadedModels.Any() ? loadedModels : availableModels;

            // Filter by constraints
            if (constraints != null)
            {
                candidates = FilterByConstraints(candidates, constraints);
            }

            // Select best model for task
            var bestModel = SelectBestModelForTask(task, candidates);

            if (bestModel == null)
            {
                return new ModelRecommendation
                {
                    ModelId = "default",
                    Reason = "No suitable model found",
                    ConfidenceScore = 0.0f,
                    AlternativeModels = new List<string>()
                };
            }

            // Find alternatives
            var alternatives = candidates
                .Where(m => m.ModelId != bestModel.ModelId && IsCompatibleWithTask(task, m))
                .Select(m => m.ModelId)
                .Take(3)
                .ToList();

            return new ModelRecommendation
            {
                ModelId = bestModel.ModelId,
                Reason = $"Best match for {task} based on type and capabilities",
                ConfidenceScore = CalculateModelConfidence(task, bestModel),
                AlternativeModels = alternatives,
                RecommendedParameters = GenerateModelParameters(task, bestModel)
            };
        }

        /// <summary>
        /// Routes a request to the appropriate model or service.
        /// </summary>
        public Task<RoutingDecision> RouteRequestAsync(
            ReasoningRequest request,
            CancellationToken cancellationToken = default)
        {
            var decision = new RoutingDecision
            {
                RequestId = request.RequestId,
                HandlerParameters = new Dictionary<string, object>()
            };

            // Determine handler based on request type
            switch (request.Type.ToLowerInvariant())
            {
                case "transcription":
                case "audio":
                    decision.SelectedHandler = "whisper";
                    decision.HandlerType = "audio";
                    break;

                case "image":
                case "vision":
                    decision.SelectedHandler = "clip";
                    decision.HandlerType = "vision";
                    break;

                case "embedding":
                case "similarity":
                    decision.SelectedHandler = "embedding";
                    decision.HandlerType = "embedding";
                    break;

                case "text":
                case "chat":
                case "completion":
                default:
                    decision.SelectedHandler = "llm";
                    decision.HandlerType = "text";
                    break;
            }

            decision.ConfidenceScore = 0.9f;

            return Task.FromResult(decision);
        }

        #endregion

        #region Private Helper Methods

        private void InitializeBuiltInPlugins()
        {
            _availablePlugins["ForensicAnalysis"] = new PluginInfo
            {
                Name = "ForensicAnalysis",
                Description = "Digital forensics and evidence analysis",
                Version = "1.0.0",
                IsLoaded = true,
                Functions = new List<string>
                {
                    "AnalyzeEvidence",
                    "ExtractMetadata",
                    "VerifyIntegrity",
                    "GenerateTimeline"
                }
            };
        }

        private string SummarizeConversation(List<InvestigationMessage> messages)
        {
            if (!messages.Any())
                return "No messages";

            var userMessages = messages.Count(m => m.Role == MessageRole.User);
            var assistantMessages = messages.Count(m => m.Role == MessageRole.Assistant);

            return $"Conversation with {userMessages} user messages and {assistantMessages} assistant responses";
        }

        private async Task ConfigureKernelForPurposeAsync(
            IKernelBuilder builder,
            string purpose,
            CancellationToken cancellationToken)
        {
            // Check if we have loaded models
            var loadedModels = await _modelOrchestrator.GetLoadedModelsAsync(cancellationToken);
            var llmModel = loadedModels.FirstOrDefault(m => m.Type == ModelType.LLM);

            if (llmModel != null)
            {
                // Configure with actual model
                _logger.LogInformation("Configuring kernel with loaded model: {ModelId}", llmModel.ModelId);

                // For Ollama models
                if (llmModel.Provider?.ToLowerInvariant() == "ollama")
                {
                    builder.AddOpenAIChatCompletion(
                        modelId: llmModel.ModelId,
                        endpoint: new Uri("http://localhost:11434"),
                        apiKey: "ollama");
                }
                // Add other providers as needed
            }
            else
            {
                _logger.LogWarning("No LLM model loaded, using mock service");
                // Use mock service for testing
                builder.Services.AddSingleton<ITextGenerationService>(
                    new MockTextGenerationService("mock-model", _logger));
            }

            // Add plugins based on purpose
            if (purpose.Contains("forensic", StringComparison.OrdinalIgnoreCase))
            {
                builder.Plugins.AddFromType<ForensicAnalysisPlugin>("ForensicAnalysis");
            }
        }

        private async Task<List<ReasoningStep>> BuildActionPlanAsync(
            IntentExtractionResult intent,
            InvestigationSession? session,
            CancellationToken cancellationToken)
        {
            var steps = new List<ReasoningStep>();

            // Build steps based on intent
            switch (intent.PrimaryIntent.ToLowerInvariant())
            {
                case "analyze":
                    steps.Add(CreateStep("Load Evidence", "LoadEvidence"));
                    steps.Add(CreateStep("Extract Features", "ExtractFeatures"));
                    steps.Add(CreateStep("Perform Analysis", "PerformAnalysis"));
                    steps.Add(CreateStep("Generate Report", "GenerateReport"));
                    break;

                case "search":
                    steps.Add(CreateStep("Parse Query", "ParseQuery"));
                    steps.Add(CreateStep("Search Evidence", "SearchEvidence"));
                    steps.Add(CreateStep("Rank Results", "RankResults"));
                    break;

                case "investigate":
                    steps.Add(CreateStep("Gather Evidence", "GatherEvidence"));
                    steps.Add(CreateStep("Analyze Patterns", "AnalyzePatterns"));
                    steps.Add(CreateStep("Identify Connections", "IdentifyConnections"));
                    steps.Add(CreateStep("Generate Findings", "GenerateFindings"));
                    break;

                default:
                    steps.Add(CreateStep("Process Query", "ProcessQuery"));
                    break;
            }

            return steps;
        }

        private ReasoningStep CreateStep(string name, string action)
        {
            return new ReasoningStep
            {
                StepId = Guid.NewGuid().ToString(),
                Name = name,
                Action = action,
                Description = $"Execute {action} operation",
                Parameters = new Dictionary<string, object>(),
                Dependencies = new List<string>(),
                IsOptional = false,
                MaxRetries = 3
            };
        }

        private async Task<StepResult> ExecuteStepAsync(
            ReasoningStep step,
            Dictionary<string, object> context,
            CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogDebug("Executing step {StepId}: {Name}", step.StepId, step.Name);

                // Simulate step execution
                await Task.Delay(100, cancellationToken);

                // In production, this would call actual services
                var output = $"Completed {step.Action}";

                return new StepResult
                {
                    StepId = step.StepId,
                    Success = true,
                    Output = output,
                    ExecutionTime = stopwatch.Elapsed
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing step {StepId}", step.StepId);

                return new StepResult
                {
                    StepId = step.StepId,
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutionTime = stopwatch.Elapsed
                };
            }
        }

        private async Task<bool> ShouldExecuteStepAsync(
            ReasoningStep step,
            Dictionary<string, object> context,
            CancellationToken cancellationToken)
        {
            // Check dependencies
            if (step.Dependencies.Any())
            {
                foreach (var dep in step.Dependencies)
                {
                    if (!context.ContainsKey($"step_{dep}_output"))
                    {
                        return false;
                    }
                }
            }

            // Check other conditions
            if (step.Parameters.ContainsKey("condition"))
            {
                // Evaluate condition
                return true; // Simplified
            }

            return true;
        }

        private async Task ExecuteAdaptiveChainAsync(
            ReasoningChain chain,
            List<StepResult> results,
            IProgress<ReasoningProgress>? progress,
            CancellationToken cancellationToken)
        {
            // Implement adaptive execution logic
            // This would dynamically adjust the execution path based on intermediate results
            foreach (var step in chain.Steps)
            {
                var result = await ExecuteStepAsync(step, chain.Context, cancellationToken);
                results.Add(result);

                // Adapt based on result
                if (!result.Success && step.Parameters.ContainsKey("fallback"))
                {
                    // Execute fallback step
                    var fallbackStep = CreateStep("Fallback", step.Parameters["fallback"].ToString()!);
                    var fallbackResult = await ExecuteStepAsync(fallbackStep, chain.Context, cancellationToken);
                    results.Add(fallbackResult);
                }
            }
        }

        private Dictionary<string, object> CompileFinalOutput(
            List<StepResult> stepResults,
            Dictionary<string, object> context)
        {
            var output = new Dictionary<string, object>
            {
                ["stepsExecuted"] = stepResults.Count,
                ["successfulSteps"] = stepResults.Count(r => r.Success),
                ["totalExecutionTime"] = stepResults.Sum(r => r.ExecutionTime.TotalMilliseconds)
            };

            // Add step outputs
            foreach (var result in stepResults.Where(r => r.Output != null))
            {
                output[$"step_{result.StepId}"] = result.Output;
            }

            return output;
        }

        private async Task<Dictionary<string, object>> ExtractEntitiesAsync(
            string query,
            CancellationToken cancellationToken)
        {
            var entities = new Dictionary<string, object>();

            // Simple entity extraction (would use NER model in production)
            if (query.Contains("email", StringComparison.OrdinalIgnoreCase))
            {
                entities["entityType"] = EntityType.Account;
            }

            if (query.Contains("person", StringComparison.OrdinalIgnoreCase) ||
                query.Contains("suspect", StringComparison.OrdinalIgnoreCase))
            {
                entities["entityType"] = EntityType.Person;
            }

            return entities;
        }

        private string DetermineIntent(string query)
        {
            var lowerQuery = query.ToLowerInvariant();

            if (lowerQuery.Contains("analyze") || lowerQuery.Contains("examine"))
                return "analyze";
            if (lowerQuery.Contains("search") || lowerQuery.Contains("find"))
                return "search";
            if (lowerQuery.Contains("investigate") || lowerQuery.Contains("explore"))
                return "investigate";
            if (lowerQuery.Contains("compare") || lowerQuery.Contains("match"))
                return "compare";
            if (lowerQuery.Contains("summarize") || lowerQuery.Contains("summary"))
                return "summarize";

            return "general";
        }

        private Dictionary<string, float> CalculateIntentScores(string query)
        {
            // Simplified scoring
            return new Dictionary<string, float>
            {
                ["analyze"] = query.Contains("analyze", StringComparison.OrdinalIgnoreCase) ? 0.9f : 0.1f,
                ["search"] = query.Contains("search", StringComparison.OrdinalIgnoreCase) ? 0.9f : 0.1f,
                ["investigate"] = query.Contains("investigate", StringComparison.OrdinalIgnoreCase) ? 0.9f : 0.1f,
                ["general"] = 0.5f
            };
        }

        private List<string> ExtractBasicEntities(string query)
        {
            var entities = new List<string>();

            // Extract quoted strings
            var quotedPattern = "\"([^\"]+)\"";
            var matches = System.Text.RegularExpressions.Regex.Matches(query, quotedPattern);
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                entities.Add(match.Groups[1].Value);
            }

            return entities;
        }

        private async Task<List<Finding>> PerformForensicAnalysisAsync(
            List<string> evidenceIds,
            Kernel kernel,
            CancellationToken cancellationToken)
        {
            // Simulate forensic analysis - return Finding objects with correct properties
            return new List<Finding>
            {
                new Finding
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Title = "Digital fingerprint detected",
                    Description = "Found matching digital signatures across evidence items",
                    Severity = FindingSeverity.High,
                    Confidence = 0.85,
                    SupportingEvidenceIds = evidenceIds,
                    RelatedEntityIds = new List<string>(),
                    DiscoveredAt = DateTimeOffset.UtcNow
                }
            };
        }

        private async Task<List<Finding>> PerformTemporalAnalysisAsync(
            List<string> evidenceIds,
            Kernel kernel,
            CancellationToken cancellationToken)
        {
            return new List<Finding>
            {
                new Finding
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Title = "Timeline pattern identified",
                    Description = "Detected temporal correlation between events",
                    Severity = FindingSeverity.Medium,
                    Confidence = 0.75,
                    SupportingEvidenceIds = evidenceIds,
                    RelatedEntityIds = new List<string>(),
                    DiscoveredAt = DateTimeOffset.UtcNow
                }
            };
        }

        private async Task<List<Finding>> PerformRelationalAnalysisAsync(
            List<string> evidenceIds,
            Kernel kernel,
            CancellationToken cancellationToken)
        {
            return new List<Finding>
            {
                new Finding
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Title = "Relationship network discovered",
                    Description = "Identified connections between entities",
                    Severity = FindingSeverity.Medium,
                    Confidence = 0.70,
                    SupportingEvidenceIds = evidenceIds,
                    RelatedEntityIds = new List<string> { "entity-001", "entity-002" },
                    DiscoveredAt = DateTimeOffset.UtcNow
                }
            };
        }

        private async Task<List<Finding>> PerformPatternAnalysisAsync(
            List<string> evidenceIds,
            Kernel kernel,
            CancellationToken cancellationToken)
        {
            return new List<Finding>
            {
                new Finding
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Title = "Behavioral pattern detected",
                    Description = "Recurring pattern identified across evidence",
                    Severity = FindingSeverity.High,
                    Confidence = 0.80,
                    SupportingEvidenceIds = evidenceIds,
                    RelatedEntityIds = new List<string>(),
                    DiscoveredAt = DateTimeOffset.UtcNow
                }
            };
        }

        private async Task<List<Finding>> PerformAnomalyDetectionAsync(
            List<string> evidenceIds,
            Kernel kernel,
            CancellationToken cancellationToken)
        {
            return new List<Finding>
            {
                new Finding
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Title = "Significant anomaly detected",
                    Description = "Unusual activity that deviates from normal patterns",
                    Severity = FindingSeverity.Critical,
                    Confidence = 0.90,
                    SupportingEvidenceIds = evidenceIds,
                    RelatedEntityIds = new List<string>(),
                    DiscoveredAt = DateTimeOffset.UtcNow
                }
            };
        }

        private List<string> GenerateRecommendations(List<Finding> findings, AnalysisType analysisType)
        {
            var recommendations = new List<string>();

            if (findings.Any(f => f.Severity == FindingSeverity.Critical))
            {
                recommendations.Add("Immediate investigation recommended for critical findings");
            }

            // Check for specific finding patterns based on title/description
            if (findings.Any(f => f.Title.Contains("network", StringComparison.OrdinalIgnoreCase) ||
                                 f.Description.Contains("connection", StringComparison.OrdinalIgnoreCase)))
            {
                recommendations.Add("Expand network analysis to identify additional connections");
            }

            if (findings.Any(f => f.Title.Contains("timeline", StringComparison.OrdinalIgnoreCase) ||
                                 f.Description.Contains("temporal", StringComparison.OrdinalIgnoreCase)))
            {
                recommendations.Add("Correlate timeline with external events for context");
            }

            recommendations.Add($"Consider follow-up {analysisType} analysis with additional evidence");

            return recommendations;
        }

        private float CalculateConfidenceScore(List<Finding> findings)
        {
            if (!findings.Any())
                return 0.0f;

            return (float)findings.Average(f => f.Confidence);
        }

        private List<string> GetPluginFunctions(Kernel kernel, string pluginName)
        {
            try
            {
                var plugin = kernel.Plugins.FirstOrDefault(p => p.Name.Equals(pluginName, StringComparison.OrdinalIgnoreCase));
                return plugin?.Select(f => f.Name).ToList() ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        private List<ModelConfiguration> FilterByConstraints(
            List<ModelConfiguration> models,
            ModelConstraints constraints)
        {
            var filtered = models.AsEnumerable();

            if (constraints.MaxMemoryBytes.HasValue)
            {
                filtered = filtered.Where(m => m.MemoryUsage <= constraints.MaxMemoryBytes.Value);
            }

            if (constraints.PreferLocal)
            {
                filtered = filtered.Where(m =>
                    m.Provider?.ToLowerInvariant() != "openai" &&
                    m.Provider?.ToLowerInvariant() != "azure");
            }

            return filtered.ToList();
        }

        private ModelConfiguration? SelectBestModelForTask(string task, List<ModelConfiguration> candidates)
        {
            var taskLower = task.ToLowerInvariant();

            // Match task to model type
            ModelType preferredType = taskLower switch
            {
                var t when t.Contains("transcribe") || t.Contains("audio") => ModelType.Whisper,
                var t when t.Contains("image") || t.Contains("vision") => ModelType.CLIP,
                var t when t.Contains("embed") || t.Contains("similarity") => ModelType.Embedding,
                var t when t.Contains("ocr") || t.Contains("text extract") => ModelType.OCR,
                _ => ModelType.LLM
            };

            // Find best match
            return candidates.FirstOrDefault(m => m.Type == preferredType) ??
                   candidates.FirstOrDefault();
        }

        private bool IsCompatibleWithTask(string task, ModelConfiguration model)
        {
            var taskLower = task.ToLowerInvariant();

            return (model.Type, taskLower) switch
            {
                (ModelType.Whisper, var t) when t.Contains("audio") || t.Contains("transcribe") => true,
                (ModelType.CLIP, var t) when t.Contains("image") || t.Contains("vision") => true,
                (ModelType.Embedding, var t) when t.Contains("embed") || t.Contains("similar") => true,
                (ModelType.LLM, _) => true, // LLMs are generally compatible with most tasks
                _ => false
            };
        }

        private float CalculateModelConfidence(string task, ModelConfiguration model)
        {
            if (IsCompatibleWithTask(task, model))
            {
                return model.Status == ModelStatus.Loaded ? 0.95f : 0.75f;
            }

            return 0.3f;
        }

        private Dictionary<string, object> GenerateModelParameters(string task, ModelConfiguration model)
        {
            var parameters = new Dictionary<string, object>();

            switch (model.Type)
            {
                case ModelType.LLM:
                    parameters["temperature"] = 0.7;
                    parameters["max_tokens"] = 2048;
                    parameters["top_p"] = 0.9;
                    break;

                case ModelType.Whisper:
                    parameters["language"] = "en";
                    parameters["task"] = "transcribe";
                    break;

                case ModelType.Embedding:
                    parameters["normalize"] = true;
                    break;
            }

            return parameters;
        }

        private string GenerateAnalysisPrompt(Dictionary<string, object> parameters)
        {
            return "Analyze the following evidence and provide detailed findings:\n" +
                   "1. Identify key patterns and anomalies\n" +
                   "2. Extract relevant entities and relationships\n" +
                   "3. Provide timeline of events if applicable\n" +
                   "4. Generate actionable recommendations";
        }

        private string GenerateSummaryPrompt(Dictionary<string, object> parameters)
        {
            return "Provide a comprehensive summary of the investigation findings:\n" +
                   "- Key discoveries\n" +
                   "- Critical evidence\n" +
                   "- Timeline of events\n" +
                   "- Recommendations for next steps";
        }

        private string GenerateExtractionPrompt(Dictionary<string, object> parameters)
        {
            return "Extract the following information from the provided content:\n" +
                   "- Named entities (people, places, organizations)\n" +
                   "- Dates and times\n" +
                   "- Financial transactions\n" +
                   "- Communication records\n" +
                   "- Any other relevant data points";
        }

        private string GenerateComparisonPrompt(Dictionary<string, object> parameters)
        {
            return "Compare the provided items and identify:\n" +
                   "- Similarities and differences\n" +
                   "- Common patterns\n" +
                   "- Discrepancies or contradictions\n" +
                   "- Correlation strength";
        }

        private string GenerateInvestigationPrompt(Dictionary<string, object> parameters)
        {
            return "Conduct a thorough investigation based on the available evidence:\n" +
                   "- Establish timeline of events\n" +
                   "- Identify all involved parties\n" +
                   "- Determine motive and opportunity\n" +
                   "- Assess credibility of sources\n" +
                   "- Generate investigative leads";
        }

        private string GenerateDefaultPrompt(string taskType, Dictionary<string, object> parameters)
        {
            return $"Perform {taskType} task with the following parameters:\n" +
                   string.Join("\n", parameters.Select(kvp => $"- {kvp.Key}: {kvp.Value}"));
        }

        private string FormatWithTemplate(ModelConfigurationTemplate template, object data)
        {
            // Use template configuration to format output
            var json = System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            return $"[{template.Name}]\n{json}";
        }

        private string FormatBasic(string templateName, object data)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            return $"[{templateName}]\n{json}";
        }

        #endregion
    }

    #region Mock Services for Testing

    /// <summary>
    /// Mock text generation service for testing without actual models.
    /// </summary>
    internal class MockTextGenerationService : ITextGenerationService
    {
        private readonly string _modelId;
        private readonly ILogger _logger;

        public IReadOnlyDictionary<string, object?> Attributes => new Dictionary<string, object?>();

        public MockTextGenerationService(string modelId, ILogger logger)
        {
            _modelId = modelId;
            _logger = logger;
        }

        public async Task<IReadOnlyList<TextContent>> GetTextContentsAsync(
            string prompt,
            PromptExecutionSettings? executionSettings = null,
            Kernel? kernel = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Mock text generation for prompt: {Prompt}", prompt);
            await Task.Delay(100, cancellationToken);

            return new List<TextContent>
            {
                new TextContent($"Mock response to: {prompt}")
            };
        }

        public async IAsyncEnumerable<StreamingTextContent> GetStreamingTextContentsAsync(
            string prompt,
            PromptExecutionSettings? executionSettings = null,
            Kernel? kernel = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Mock streaming generation for prompt: {Prompt}", prompt);

            var response = $"Mock streaming response to: {prompt}";
            foreach (var word in response.Split(' '))
            {
                await Task.Delay(50, cancellationToken);
                yield return new StreamingTextContent(word + " ");
            }
        }
    }



    #endregion
}