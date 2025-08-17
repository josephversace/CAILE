using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IIM.Core.Models;
using IIM.Shared.Enums;
using Microsoft.Extensions.Logging;

namespace IIM.Core.Services
{
    public class InvestigationService : IInvestigationService
    {
        private readonly ILogger<InvestigationService> _logger;
        private readonly Dictionary<string, InvestigationSession> _sessions = new();
        private readonly Dictionary<string, Case> _cases = new();

        public InvestigationService(ILogger<InvestigationService> logger)
        {
            _logger = logger;
            InitializeSampleData();
        }

        public Task<InvestigationSession> CreateSessionAsync(CreateSessionRequest request, CancellationToken cancellationToken = default)
        {
            var session = new InvestigationSession
            {
                Id = Guid.NewGuid().ToString(),
                CaseId = request.CaseId,
                Title = request.Title,
                Type = Enum.Parse<InvestigationType>(request.InvestigationType),
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                Status = InvestigationStatus.Active,
                EnabledTools = GetDefaultTools(),
                Models = GetDefaultModels()
            };

            _sessions[session.Id] = session;
            _logger.LogInformation("Created investigation session {SessionId} for case {CaseId}", session.Id, request.CaseId);

            return Task.FromResult(session);
        }

        public Task<InvestigationSession> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            if (_sessions.TryGetValue(sessionId, out var session))
            {
                return Task.FromResult(session);
            }

            throw new KeyNotFoundException($"Session {sessionId} not found");
        }

        public Task<List<InvestigationSession>> GetSessionsByCaseAsync(string caseId, CancellationToken cancellationToken = default)
        {
            var sessions = _sessions.Values
                .Where(s => s.CaseId == caseId)
                .OrderByDescending(s => s.UpdatedAt)
                .ToList();

            return Task.FromResult(sessions);
        }

        public Task<bool> DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            var removed = _sessions.Remove(sessionId);
            if (removed)
            {
                _logger.LogInformation("Deleted investigation session {SessionId}", sessionId);
            }
            return Task.FromResult(removed);
        }

        public async Task<InvestigationResponse> ProcessQueryAsync(string sessionId, InvestigationQuery query, CancellationToken cancellationToken = default)
        {
            var session = await GetSessionAsync(sessionId, cancellationToken);

            // Add user message to session
            var userMessage = new InvestigationMessage
            {
                Role = MessageRole.User,
                Content = query.Text,
                Attachments = query.Attachments,
                Timestamp = DateTimeOffset.UtcNow
            };
            session.Messages.Add(userMessage);

            // Process the query (in production, this would call AI models)
            var response = new InvestigationResponse
            {
                Message = $"Processing query: {query.Text}"
            };

            // Check if tools should be executed
            if (query.EnabledTools.Any())
            {
                foreach (var tool in query.EnabledTools)
                {
                    var toolResult = await ExecuteToolAsync(sessionId, tool, new Dictionary<string, object>(), cancellationToken);
                    response.ToolResults.Add(toolResult);
                }
            }

            // Add assistant response to session
            var assistantMessage = new InvestigationMessage
            {
                Role = MessageRole.Assistant,
                Content = response.Message,
                ToolResults = response.ToolResults,
                Citations = response.Citations,
                Timestamp = DateTimeOffset.UtcNow,
                ModelUsed = session.Models.Values.FirstOrDefault()?.ModelId
            };
            session.Messages.Add(assistantMessage);

            // Update session timestamp
            session.UpdatedAt = DateTimeOffset.UtcNow;

            _logger.LogInformation("Processed query for session {SessionId}", sessionId);

            return response;
        }

        public Task<ToolResult> ExecuteToolAsync(string sessionId, string toolName, Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Executing tool {ToolName} for session {SessionId}", toolName, sessionId);

            var startTime = DateTimeOffset.UtcNow;

            // Simulate tool execution with your existing ToolResult model
            var result = new ToolResult
            {
                Id = Guid.NewGuid().ToString("N"),
                ToolName = toolName,
                Status = ToolStatus.Success,
                Data = new
                {
                    data = "Tool execution result",
                    timestamp = DateTimeOffset.UtcNow,
                    parameters = parameters
                },
                ExecutedAt = startTime,
                ExecutionTime = TimeSpan.FromMilliseconds(Random.Shared.Next(100, 1000)),
                Metadata = new Dictionary<string, object>
                {
                    ["sessionId"] = sessionId,
                    ["executedBy"] = Environment.UserName
                }
            };

            // Add visualizations for certain tools
            if (toolName == "image_analysis")
            {
                result.Visualizations.Add(new Visualization
                {
                    Type = "chart",
                    Title = "Image Analysis Results",
                    Description = "Object detection and classification results",
                    Data = new
                    {
                        labels = new[] { "Person", "Vehicle", "Building" },
                        values = new[] { 3, 2, 5 }
                    },
                    RenderFormat = "canvas"
                });
            }
            else if (toolName == "entity_recognition")
            {
                result.Visualizations.Add(new Visualization
                {
                    Type = "graph",
                    Title = "Entity Relationship Graph",
                    Description = "Connections between identified entities",
                    Data = new
                    {
                        nodes = new[]
                        {
                            new { id = 1, label = "John Doe" },
                            new { id = 2, label = "ABC Corp" }
                        },
                        edges = new[]
                        {
                            new { from = 1, to = 2, label = "Employee" }
                        }
                    },
                    RenderFormat = "svg"
                });
            }

            // Add recommendations based on tool
            switch (toolName)
            {
                case "web_search":
                    result.Recommendations.Add("Review top 5 search results for relevance");
                    result.Recommendations.Add("Consider expanding search terms");
                    break;
                case "document_extraction":
                    result.Recommendations.Add("Verify extracted metadata accuracy");
                    result.Recommendations.Add("Check for hidden or embedded content");
                    break;
                case "image_analysis":
                    result.Recommendations.Add("Manual review recommended for faces detected");
                    result.Recommendations.Add("Consider enhanced image processing for low quality regions");
                    break;
            }

            return Task.FromResult(result);
        }

        public Task<List<Case>> GetRecentCasesAsync(int count = 10, CancellationToken cancellationToken = default)
        {
            var cases = _cases.Values
                .OrderByDescending(c => c.UpdatedAt)
                .Take(count)
                .ToList();

            return Task.FromResult(cases);
        }

        public Task<List<InvestigationSession>> GetCaseSessionsAsync(string caseId, CancellationToken cancellationToken = default)
        {
            return GetSessionsByCaseAsync(caseId, cancellationToken);
        }

        // Method overload for ProcessQueryAsync that accepts just sessionId and query text
        public Task<InvestigationResponse> ProcessQueryAsync(string sessionId, string queryText, CancellationToken cancellationToken = default)
        {
            var query = new InvestigationQuery
            {
                Text = queryText,
                EnabledTools = GetDefaultTools()
            };

            return ProcessQueryAsync(sessionId, query, cancellationToken);
        }

        // Method overload for ProcessQueryAsync with attachments
        public Task<InvestigationResponse> ProcessQueryAsync(
            string sessionId,
            string queryText,
            List<Attachment> attachments,
            CancellationToken cancellationToken = default)
        {
            var query = new InvestigationQuery
            {
                Text = queryText,
                Attachments = attachments,
                EnabledTools = GetDefaultTools()
            };

            return ProcessQueryAsync(sessionId, query, cancellationToken);
        }

        private List<string> GetDefaultTools()
        {
            return new List<string>
            {
                "web_search",
                "image_analysis",
                "document_extraction",
                "entity_recognition",
                "sentiment_analysis",
                "translation"
            };
        }

        private Dictionary<string, ModelConfiguration> GetDefaultModels()
        {
            return new Dictionary<string, ModelConfiguration>
            {
                ["text"] = new ModelConfiguration
                {
                    ModelId = "llama3.1:70b",
                    Provider = "Ollama",
                    Type = ModelType.LLM,
                    Status = ModelStatus.Available,
                    MemoryUsage = 0,
                    Parameters = new Dictionary<string, object>
                    {
                        ["temperature"] = 0.7,
                        ["max_tokens"] = 4096,
                        ["top_p"] = 0.9,
                        ["frequency_penalty"] = 0.0,
                        ["presence_penalty"] = 0.0,
                        ["seed"] = 42
                    },
                    Capabilities = new ModelCapabilities
                    {
                        MaxContextLength = 131072,
                        SupportsStreaming = true,
                        SupportsFineTuning = false,
                        SupportsMultiModal = false,
                        SupportedLanguages = new List<string> { "en", "es", "fr", "de", "it", "pt", "ru", "zh", "ja", "ko" },
                        SpecialFeatures = new List<string>
                        {
                            "code_generation",
                            "reasoning",
                            "instruction_following",
                            "multi_turn_conversation",
                            "tool_use"
                        },
                        CustomCapabilities = new Dictionary<string, object>
                        {
                            ["supports_json_mode"] = true,
                            ["supports_function_calling"] = true,
                            ["supports_system_prompts"] = true
                        }
                    }
                },
                ["vision"] = new ModelConfiguration
                {
                    ModelId = "llava:34b",
                    Provider = "Ollama",
                    Type = ModelType.LLM, // Multi-modal LLM
                    Status = ModelStatus.Available,
                    MemoryUsage = 0,
                    Parameters = new Dictionary<string, object>
                    {
                        ["temperature"] = 0.5,
                        ["max_tokens"] = 2048,
                        ["image_detail"] = "high"
                    },
                    Capabilities = new ModelCapabilities
                    {
                        MaxContextLength = 32768,
                        SupportsStreaming = true,
                        SupportsFineTuning = false,
                        SupportsMultiModal = true,
                        SupportedLanguages = new List<string> { "en" },
                        SpecialFeatures = new List<string>
                        {
                            "image_understanding",
                            "visual_qa",
                            "ocr",
                            "object_detection",
                            "scene_description"
                        },
                        CustomCapabilities = new Dictionary<string, object>
                        {
                            ["max_image_size"] = "4096x4096",
                            ["supported_formats"] = new[] { "jpg", "png", "bmp", "gif", "webp" },
                            ["batch_processing"] = true
                        }
                    }
                },
                ["audio"] = new ModelConfiguration
                {
                    ModelId = "whisper-large",
                    Provider = "LocalWhisper",
                    Type = ModelType.Whisper,
                    Status = ModelStatus.Available,
                    MemoryUsage = 0,
                    Parameters = new Dictionary<string, object>
                    {
                        ["language"] = "auto",
                        ["task"] = "transcribe",
                        ["temperature"] = 0.0,
                        ["beam_size"] = 5,
                        ["best_of"] = 5,
                        ["vad_filter"] = true,
                        ["word_timestamps"] = true
                    },
                    Capabilities = new ModelCapabilities
                    {
                        MaxContextLength = 30000, // 30 seconds of audio
                        SupportsStreaming = true,
                        SupportsFineTuning = false,
                        SupportsMultiModal = false,
                        SupportedLanguages = new List<string>
                        {
                            "en", "zh", "de", "es", "ru", "ko", "fr", "ja", "pt", "tr",
                            "pl", "ca", "nl", "ar", "sv", "it", "id", "hi", "fi", "vi",
                            "he", "uk", "el", "ms", "cs", "ro", "da", "hu", "ta", "no",
                            "th", "ur", "hr", "bg", "lt", "la", "mi", "ml", "cy", "sk",
                            "te", "fa", "lv", "bn", "sr", "az", "sl", "kn", "et", "mk",
                            "br", "eu", "is", "hy", "ne", "mn", "bs", "kk", "sq", "sw",
                            "gl", "mr", "pa", "si", "km", "sn", "yo", "so", "af", "oc",
                            "ka", "be", "tg", "sd", "gu", "am", "yi", "lo", "uz", "fo",
                            "ht", "ps", "tk", "nn", "mt", "sa", "lb", "my", "bo", "tl",
                            "mg", "as", "tt", "haw", "ln", "ha", "ba", "jw", "su"
                        },
                        SpecialFeatures = new List<string>
                        {
                            "speech_recognition",
                            "speaker_diarization",
                            "language_detection",
                            "timestamp_generation",
                            "translation"
                        },
                        CustomCapabilities = new Dictionary<string, object>
                        {
                            ["supported_audio_formats"] = new[] { "wav", "mp3", "m4a", "flac", "ogg", "opus", "webm" },
                            ["max_file_size_mb"] = 1000,
                            ["supports_realtime"] = true,
                            ["supports_batch"] = true
                        }
                    }
                },
                ["embedding"] = new ModelConfiguration
                {
                    ModelId = "nomic-embed-text",
                    Provider = "Ollama",
                    Type = ModelType.Embedding,
                    Status = ModelStatus.Available,
                    MemoryUsage = 0,
                    Parameters = new Dictionary<string, object>
                    {
                        ["dimensions"] = 768,
                        ["normalize"] = true,
                        ["truncate"] = true
                    },
                    Capabilities = new ModelCapabilities
                    {
                        MaxContextLength = 8192,
                        SupportsStreaming = false,
                        SupportsFineTuning = false,
                        SupportsMultiModal = false,
                        SupportedLanguages = new List<string> { "en" },
                        SpecialFeatures = new List<string>
                        {
                            "semantic_search",
                            "document_retrieval",
                            "clustering",
                            "classification"
                        },
                        CustomCapabilities = new Dictionary<string, object>
                        {
                            ["embedding_dimensions"] = 768,
                            ["batch_size"] = 512,
                            ["supports_matryoshka"] = true
                        }
                    }
                },
                ["ocr"] = new ModelConfiguration
                {
                    ModelId = "tesseract-5.0",
                    Provider = "LocalTesseract",
                    Type = ModelType.OCR,
                    Status = ModelStatus.Available,
                    MemoryUsage = 0,
                    Parameters = new Dictionary<string, object>
                    {
                        ["language"] = "eng",
                        ["psm"] = 3, // Page segmentation mode
                        ["oem"] = 3, // OCR Engine mode
                        ["preserve_interword_spaces"] = true
                    },
                    Capabilities = new ModelCapabilities
                    {
                        MaxContextLength = 0, // Image-based, not token-based
                        SupportsStreaming = false,
                        SupportsFineTuning = false,
                        SupportsMultiModal = false,
                        SupportedLanguages = new List<string>
                        {
                            "eng", "chi_sim", "chi_tra", "jpn", "kor", "ara", "rus",
                            "deu", "fra", "spa", "ita", "por", "nld", "pol", "tur"
                        },
                        SpecialFeatures = new List<string>
                        {
                            "text_extraction",
                            "layout_analysis",
                            "table_detection",
                            "handwriting_recognition"
                        },
                        CustomCapabilities = new Dictionary<string, object>
                        {
                            ["supported_image_formats"] = new[] { "png", "jpg", "tiff", "bmp", "pnm", "gif", "webp" },
                            ["max_image_size"] = "10000x10000",
                            ["supports_pdf"] = true,
                            ["supports_multi_page"] = true
                        }
                    }
                }
            };
        }

        private void InitializeSampleData()
        {
            // Add sample cases
            _cases["case-001"] = new Case
            {
                Id = "case-001",
        
                Name = "Digital Evidence Investigation",
                Status = CaseStatus.Open,
                Priority = CasePriority.High,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-5),
                UpdatedAt = DateTimeOffset.UtcNow,
                LeadInvestigator = Environment.UserName
            };

            _cases["case-002"] = new Case
            {
                Id = "case-002",
       
                Name = "Fraud Investigation",
                Status = CaseStatus.InProgress,
                Priority = CasePriority.Medium,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-10),
                UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1),
                LeadInvestigator = Environment.UserName
            };
        }
    }
}