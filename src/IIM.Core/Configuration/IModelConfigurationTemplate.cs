using IIM.Core.AI;
using IIM.Core.Configuration;
using IIM.Core.Models;
using IIM.Infrastructure.Storage;
using IIM.Shared.Enums;
using IIM.Shared.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;


namespace IIM.Core.Services.Configuration;

/// <summary>
/// Represents a saved model configuration template for investigations.
/// Users can create, save, and reuse these templates for different investigation types.
/// Similar to LMStudio's model selection profiles.
/// </summary>
public class ModelConfigurationTemplate
{
    /// <summary>
    /// Unique identifier for the template
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// User-friendly name for the template (e.g., "Fast Response", "Deep Analysis")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of what this template is optimized for
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Category for organization (e.g., "Financial Crime", "CSAM", "Fraud", "General")
    /// </summary>
    public string Category { get; set; } = "General";

    /// <summary>
    /// Icon or emoji for UI display
    /// </summary>
    public string Icon { get; set; } = "🔍";

    /// <summary>
    /// Tags for searchability and filtering
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Model configurations for each capability.
    /// Key: capability type (text, vision, audio, embedder, etc.)
    /// Value: Model configuration with all settings
    /// This allows users to swap models for each capability independently
    /// </summary>
    public Dictionary<string, ModelSelectionConfig> Models { get; set; } = new();

    /// <summary>
    /// Pipeline configuration for how models work together
    /// </summary>
    public PipelineConfig Pipeline { get; set; } = new();

    /// <summary>
    /// Tool configurations - which investigation tools are enabled and their settings
    /// </summary>
    public Dictionary<string, ToolConfig> Tools { get; set; } = new();

    /// <summary>
    /// Performance preferences for this template
    /// </summary>
    public PerformancePreferences Performance { get; set; } = new();

    /// <summary>
    /// Template metadata for tracking usage and updates
    /// </summary>
    public TemplateMetadata Metadata { get; set; } = new();

    /// <summary>
    /// Whether this is a system-provided template (read-only) or user-created
    /// </summary>
    public bool IsSystemTemplate { get; set; } = false;

    /// <summary>
    /// Template version for compatibility tracking
    /// </summary>
    public string Version { get; set; } = "1.0.0";
}

/// <summary>
/// Model selection and configuration for a specific capability
/// </summary>
public class ModelSelectionConfig
{
    /// <summary>
    /// The primary model ID to use (e.g., "llama3.1:70b", "whisper-large-v3")
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Alternative model IDs in priority order (fallbacks if primary isn't available)
    /// </summary>
    public List<string> AlternativeModels { get; set; } = new();

    /// <summary>
    /// Model-specific parameters (temperature, max_tokens, etc.)
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Whether to automatically load this model when template is selected
    /// </summary>
    public bool AutoLoad { get; set; } = true;

    /// <summary>
    /// Minimum memory required in bytes
    /// </summary>
    public long MinMemoryRequired { get; set; }

    /// <summary>
    /// Preferred device for this model (GPU, CPU, Auto)
    /// </summary>
    public string PreferredDevice { get; set; } = "Auto";
}

/// <summary>
/// Pipeline configuration for model orchestration
/// </summary>
public class PipelineConfig
{
    /// <summary>
    /// RAG-specific configuration
    /// </summary>
    public RagPipelineConfig? Rag { get; set; }

    /// <summary>
    /// Multi-modal pipeline configuration
    /// </summary>
    public MultiModalConfig? MultiModal { get; set; }

    /// <summary>
    /// Default processing order for tools/models
    /// </summary>
    public List<string> ProcessingOrder { get; set; } = new();
}

/// <summary>
/// RAG pipeline configuration
/// </summary>
public class RagPipelineConfig
{
    public int ChunkSize { get; set; } = 512;
    public int ChunkOverlap { get; set; } = 50;
    public int TopK { get; set; } = 5;
    public float MinRelevanceScore { get; set; } = 0.7f;
    public bool UseReranking { get; set; } = true;
    public string? RerankingModel { get; set; }
}

/// <summary>
/// Multi-modal pipeline configuration
/// </summary>
public class MultiModalConfig
{
    public bool EnableCrossModalSearch { get; set; } = true;
    public bool AutoTranscribeAudio { get; set; } = true;
    public bool AutoAnalyzeImages { get; set; } = true;
    public bool AutoExtractText { get; set; } = true;
}

/// <summary>
/// Tool configuration for investigation tools
/// </summary>
public class ToolConfig
{
    public bool Enabled { get; set; } = true;
    public Dictionary<string, object> Settings { get; set; } = new();
}

/// <summary>
/// Performance preferences for the template
/// </summary>
public class PerformancePreferences
{
    /// <summary>
    /// Priority: Speed, Quality, or Balanced
    /// </summary>
    public string Priority { get; set; } = "Balanced";

    /// <summary>
    /// Maximum number of models loaded concurrently
    /// </summary>
    public int MaxConcurrentModels { get; set; } = 3;

    /// <summary>
    /// Maximum memory usage in bytes
    /// </summary>
    public long MaxMemoryUsage { get; set; } = 64L * 1024 * 1024 * 1024; // 64GB default

    /// <summary>
    /// Enable response streaming for real-time updates
    /// </summary>
    public bool EnableStreaming { get; set; } = true;
}

/// <summary>
/// Template metadata for tracking and analytics
/// </summary>
public class TemplateMetadata
{
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string ModifiedBy { get; set; } = string.Empty;
    public DateTimeOffset ModifiedAt { get; set; } = DateTimeOffset.UtcNow;
    public int UsageCount { get; set; } = 0;
    public List<string> RecentCases { get; set; } = new();
}

/// <summary>
/// Service for managing model configuration templates.
/// This service handles CRUD operations for templates and applies them to investigation sessions.
/// </summary>
public interface IModelConfigurationTemplateService
{
    /// <summary>
    /// Creates a new template from the current session configuration
    /// </summary>
    Task<ModelConfigurationTemplate> CreateTemplateFromSessionAsync(
        string sessionId,
        string templateName,
        string description,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a template to storage
    /// </summary>
    Task<ModelConfigurationTemplate> SaveTemplateAsync(
        ModelConfigurationTemplate template,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a template by ID
    /// </summary>
    Task<ModelConfigurationTemplate?> GetTemplateAsync(
        string templateId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all templates, optionally filtered by category
    /// </summary>
    Task<List<ModelConfigurationTemplate>> GetTemplatesAsync(
        string? category = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing template
    /// </summary>
    Task<ModelConfigurationTemplate> UpdateTemplateAsync(
        ModelConfigurationTemplate template,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a template (user templates only, not system templates)
    /// </summary>
    Task<bool> DeleteTemplateAsync(
        string templateId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clones an existing template with a new name
    /// </summary>
    Task<ModelConfigurationTemplate> CloneTemplateAsync(
        string templateId,
        string newName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a template to an investigation session
    /// </summary>
    Task<InvestigationSession> ApplyTemplateToSessionAsync(
        string templateId,
        string sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads all models specified in a template
    /// </summary>
    Task<bool> LoadModelsFromTemplateAsync(
        string templateId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets system-provided templates
    /// </summary>
    Task<List<ModelConfigurationTemplate>> GetSystemTemplatesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports a template to JSON
    /// </summary>
    Task<string> ExportTemplateAsync(
        string templateId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports a template from JSON
    /// </summary>
    Task<ModelConfigurationTemplate> ImportTemplateAsync(
        string json,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of the model configuration template service.
/// This service integrates with existing storage infrastructure and services.
/// </summary>
public class ModelConfigurationTemplateService : IModelConfigurationTemplateService
{
    private readonly ILogger<ModelConfigurationTemplateService> _logger;
    private readonly StorageConfiguration _storageConfig;
    private readonly IModelOrchestrator _modelOrchestrator;
    private readonly ISessionProvider _investigationService;
    private readonly string _templatesPath;
    private readonly Dictionary<string, ModelConfigurationTemplate> _templateCache = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Initializes the template service with required dependencies
    /// </summary>
    public ModelConfigurationTemplateService(
        ILogger<ModelConfigurationTemplateService> logger,
        StorageConfiguration storageConfig,
        IModelOrchestrator modelOrchestrator,
        ISessionProvider investigationService)
    {
        _logger = logger;
        _storageConfig = storageConfig;
        _modelOrchestrator = modelOrchestrator;
        _investigationService = investigationService;

        // Use centralized storage configuration
        _templatesPath = Path.Combine(_storageConfig.BasePath, "Templates");
        Directory.CreateDirectory(_templatesPath);

        // Initialize system templates on startup
        _ = InitializeSystemTemplatesAsync();
    }

    /// <summary>
    /// Creates a template from an existing investigation session's configuration
    /// </summary>
    public async Task<ModelConfigurationTemplate> CreateTemplateFromSessionAsync(
        string sessionId,
        string templateName,
        string description,
        CancellationToken cancellationToken = default)
    {
        var session = await _investigationService.GetSessionAsync(sessionId, cancellationToken);

        var template = new ModelConfigurationTemplate
        {
            Name = templateName,
            Description = description,
            Category = DetermineCategoryFromSession(session),
            Models = ConvertSessionModelsToTemplate(session.Models),
            Tools = ConvertSessionToolsToTemplate(session.EnabledTools),
            Metadata = new TemplateMetadata
            {
                CreatedBy = session.CreatedBy,
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        return await SaveTemplateAsync(template, cancellationToken);
    }

    /// <summary>
    /// Saves a template to the file system
    /// </summary>
    public async Task<ModelConfigurationTemplate> SaveTemplateAsync(
        ModelConfigurationTemplate template,
        CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            // Validate template
            ValidateTemplate(template);

            // Update metadata
            if (string.IsNullOrEmpty(template.Id))
            {
                template.Id = Guid.NewGuid().ToString("N");
                template.Metadata.CreatedAt = DateTimeOffset.UtcNow;
            }
            template.Metadata.ModifiedAt = DateTimeOffset.UtcNow;

            // Save to file system
            var filePath = GetTemplateFilePath(template.Id);
            var json = JsonSerializer.Serialize(template, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json, cancellationToken);

            // Update cache
            _templateCache[template.Id] = template;

            _logger.LogInformation("Saved template {TemplateId}: {TemplateName}",
                template.Id, template.Name);

            return template;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Retrieves a template by ID from cache or storage
    /// </summary>
    public async Task<ModelConfigurationTemplate?> GetTemplateAsync(
        string templateId,
        CancellationToken cancellationToken = default)
    {
        // Check cache first
        if (_templateCache.TryGetValue(templateId, out var cached))
        {
            return cached;
        }

        // Load from storage
        var filePath = GetTemplateFilePath(templateId);
        if (!File.Exists(filePath))
        {
            return null;
        }

        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        var template = JsonSerializer.Deserialize<ModelConfigurationTemplate>(json, _jsonOptions);

        if (template != null)
        {
            _templateCache[templateId] = template;
        }

        return template;
    }

    /// <summary>
    /// Gets all templates, optionally filtered by category
    /// </summary>
    public async Task<List<ModelConfigurationTemplate>> GetTemplatesAsync(
        string? category = null,
        CancellationToken cancellationToken = default)
    {
        var templates = new List<ModelConfigurationTemplate>();

        if (!Directory.Exists(_templatesPath))
        {
            return templates;
        }

        var files = Directory.GetFiles(_templatesPath, "*.json");
        foreach (var file in files)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file, cancellationToken);
                var template = JsonSerializer.Deserialize<ModelConfigurationTemplate>(json, _jsonOptions);

                if (template != null)
                {
                    if (string.IsNullOrEmpty(category) || template.Category == category)
                    {
                        templates.Add(template);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load template from {File}", file);
            }
        }

        return templates.OrderBy(t => t.Category).ThenBy(t => t.Name).ToList();
    }

    /// <summary>
    /// Updates an existing template (user templates only)
    /// </summary>
    public async Task<ModelConfigurationTemplate> UpdateTemplateAsync(
        ModelConfigurationTemplate template,
        CancellationToken cancellationToken = default)
    {
        // Don't allow updating system templates
        var existing = await GetTemplateAsync(template.Id, cancellationToken);
        if (existing?.IsSystemTemplate == true)
        {
            throw new InvalidOperationException("Cannot modify system templates. Clone it first.");
        }

        template.Version = IncrementVersion(template.Version);
        return await SaveTemplateAsync(template, cancellationToken);
    }

    /// <summary>
    /// Deletes a user template (system templates cannot be deleted)
    /// </summary>
    public async Task<bool> DeleteTemplateAsync(
        string templateId,
        CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var template = await GetTemplateAsync(templateId, cancellationToken);
            if (template?.IsSystemTemplate == true)
            {
                throw new InvalidOperationException("Cannot delete system templates");
            }

            var filePath = GetTemplateFilePath(templateId);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _templateCache.Remove(templateId);
                _logger.LogInformation("Deleted template {TemplateId}", templateId);
                return true;
            }

            return false;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Creates a copy of an existing template with a new name
    /// </summary>
    public async Task<ModelConfigurationTemplate> CloneTemplateAsync(
        string templateId,
        string newName,
        CancellationToken cancellationToken = default)
    {
        var original = await GetTemplateAsync(templateId, cancellationToken);
        if (original == null)
        {
            throw new KeyNotFoundException($"Template {templateId} not found");
        }

        // Deep copy via serialization
        var json = JsonSerializer.Serialize(original, _jsonOptions);
        var clone = JsonSerializer.Deserialize<ModelConfigurationTemplate>(json, _jsonOptions)!;

        // Update clone properties
        clone.Id = Guid.NewGuid().ToString("N");
        clone.Name = newName;
        clone.IsSystemTemplate = false;
        clone.Metadata = new TemplateMetadata
        {
            CreatedBy = Environment.UserName,
            CreatedAt = DateTimeOffset.UtcNow
        };

        return await SaveTemplateAsync(clone, cancellationToken);
    }

    /// <summary>
    /// Applies a template configuration to an investigation session
    /// </summary>
    public async Task<InvestigationSession> ApplyTemplateToSessionAsync(
        string templateId,
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        var template = await GetTemplateAsync(templateId, cancellationToken);
        if (template == null)
        {
            throw new KeyNotFoundException($"Template {templateId} not found");
        }

        var session = await _investigationService.GetSessionAsync(sessionId, cancellationToken);

        // Clear existing models and apply template models
        session.Models.Clear();
        foreach (var (capability, config) in template.Models)
        {
            session.Models[capability] = new ModelConfiguration
            {
                ModelId = config.ModelId,
                Parameters = config.Parameters,
                Status = ModelStatus.Available,
                Type = DetermineModelType(config.ModelId)
            };
        }

        // Apply tools
        session.EnabledTools = template.Tools
            .Where(t => t.Value.Enabled)
            .Select(t => t.Key)
            .ToList();

        // Track usage
        template.Metadata.UsageCount++;
        template.Metadata.RecentCases.Add(session.CaseId);
        if (template.Metadata.RecentCases.Count > 10)
        {
            template.Metadata.RecentCases.RemoveAt(0);
        }
        await SaveTemplateAsync(template, cancellationToken);

        _logger.LogInformation("Applied template {TemplateId} to session {SessionId}",
            templateId, sessionId);

        return session;
    }

    /// <summary>
    /// Loads all models specified in a template
    /// </summary>
    public async Task<bool> LoadModelsFromTemplateAsync(
        string templateId,
        CancellationToken cancellationToken = default)
    {
        var template = await GetTemplateAsync(templateId, cancellationToken);
        if (template == null)
        {
            throw new KeyNotFoundException($"Template {templateId} not found");
        }

        var loadedCount = 0;
        var failedModels = new List<string>();

        // Sort models by load priority
        var modelsToLoad = template.Models
            .Where(m => m.Value.AutoLoad)
            .OrderBy(m => GetLoadPriority(m.Key));

        foreach (var (capability, config) in modelsToLoad)
        {
            try
            {
                // Try primary model first
                var loaded = await TryLoadModelAsync(config.ModelId, config.Parameters, cancellationToken);

                // If failed, try alternatives
                if (!loaded && config.AlternativeModels.Any())
                {
                    foreach (var altModel in config.AlternativeModels)
                    {
                        loaded = await TryLoadModelAsync(altModel, config.Parameters, cancellationToken);
                        if (loaded)
                        {
                            _logger.LogInformation("Loaded alternative model {ModelId} for {Capability}",
                                altModel, capability);
                            break;
                        }
                    }
                }

                if (loaded)
                {
                    loadedCount++;
                }
                else
                {
                    failedModels.Add($"{capability}: {config.ModelId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load model {ModelId} for {Capability}",
                    config.ModelId, capability);
                failedModels.Add($"{capability}: {config.ModelId}");
            }
        }

        if (failedModels.Any())
        {
            _logger.LogWarning("Failed to load models: {Models}", string.Join(", ", failedModels));
        }

        return failedModels.Count == 0;
    }

    /// <summary>
    /// Gets all system-provided templates
    /// </summary>
    public async Task<List<ModelConfigurationTemplate>> GetSystemTemplatesAsync(
        CancellationToken cancellationToken = default)
    {
        var allTemplates = await GetTemplatesAsync(null, cancellationToken);
        return allTemplates.Where(t => t.IsSystemTemplate).ToList();
    }

    /// <summary>
    /// Exports a template to JSON string
    /// </summary>
    public async Task<string> ExportTemplateAsync(
        string templateId,
        CancellationToken cancellationToken = default)
    {
        var template = await GetTemplateAsync(templateId, cancellationToken);
        if (template == null)
        {
            throw new KeyNotFoundException($"Template {templateId} not found");
        }

        return JsonSerializer.Serialize(template, _jsonOptions);
    }

    /// <summary>
    /// Imports a template from JSON string
    /// </summary>
    public async Task<ModelConfigurationTemplate> ImportTemplateAsync(
        string json,
        CancellationToken cancellationToken = default)
    {
        var template = JsonSerializer.Deserialize<ModelConfigurationTemplate>(json, _jsonOptions);
        if (template == null)
        {
            throw new InvalidOperationException("Invalid template JSON");
        }

        // Generate new ID to avoid conflicts
        template.Id = Guid.NewGuid().ToString("N");
        template.IsSystemTemplate = false;

        return await SaveTemplateAsync(template, cancellationToken);
    }

    // Helper Methods

    /// <summary>
    /// Initializes system-provided templates
    /// </summary>
    private async Task InitializeSystemTemplatesAsync()
    {
        var systemTemplates = GetDefaultSystemTemplates();

        foreach (var template in systemTemplates)
        {
            template.IsSystemTemplate = true;

            // Check if already exists
            var existing = await GetTemplateAsync(template.Id, CancellationToken.None);
            if (existing == null)
            {
                await SaveTemplateAsync(template, CancellationToken.None);
                _logger.LogInformation("Initialized system template: {TemplateName}", template.Name);
            }
        }
    }

    /// <summary>
    /// Validates a template before saving
    /// </summary>
    private void ValidateTemplate(ModelConfigurationTemplate template)
    {
        if (string.IsNullOrWhiteSpace(template.Name))
        {
            throw new ArgumentException("Template name is required");
        }

        if (!template.Models.Any())
        {
            throw new ArgumentException("At least one model must be configured");
        }
    }

    /// <summary>
    /// Gets the file path for a template
    /// </summary>
    private string GetTemplateFilePath(string templateId)
    {
        return Path.Combine(_templatesPath, $"{templateId}.json");
    }

    /// <summary>
    /// Attempts to load a model with given parameters
    /// </summary>
    private async Task<bool> TryLoadModelAsync(
        string modelId,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = new ModelRequest
            {
                ModelId = modelId,
                ModelPath = Path.Combine(_storageConfig.ModelsPath, modelId),
                ModelType = DetermineModelType(modelId),
                Options = parameters
            };

            var handle = await _modelOrchestrator.LoadModelAsync(request, null, cancellationToken);
            return handle != null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load model {ModelId}", modelId);
            return false;
        }
    }

    /// <summary>
    /// Determines the load priority for a capability
    /// </summary>
    private int GetLoadPriority(string capability) => capability switch
    {
        "embedder" => 1,  // Load first for RAG
        "text" => 2,      // Main LLM second
        "vision" => 3,    // Vision models third
        "audio" => 4,     // Audio models fourth
        _ => 99
    };

    /// <summary>
    /// Increments the patch version number
    /// </summary>
    private string IncrementVersion(string version)
    {
        var parts = version.Split('.');
        if (parts.Length == 3 && int.TryParse(parts[2], out var patch))
        {
            return $"{parts[0]}.{parts[1]}.{patch + 1}";
        }
        return version;
    }

    /// <summary>
    /// Determines the model type from the model ID
    /// </summary>
    private ModelType DetermineModelType(string modelId)
    {
        var lower = modelId.ToLowerInvariant();

        if (lower.Contains("whisper")) return ModelType.Whisper;
        if (lower.Contains("clip") || lower.Contains("vit")) return ModelType.CLIP;
        if (lower.Contains("embed") || lower.Contains("bge") || lower.Contains("minilm")) return ModelType.Embedding;
        if (lower.Contains("ocr") || lower.Contains("tesseract")) return ModelType.OCR;

        return ModelType.LLM; // Default to LLM
    }

    /// <summary>
    /// Determines category based on session type
    /// </summary>
    private string DetermineCategoryFromSession(InvestigationSession session)
    {
        return session.Type switch
        {
            InvestigationType.Financial => "Financial",
            InvestigationType.Cybercrime => "Cyber",
            InvestigationType.OSINTResearch => "OSINT",
            InvestigationType.ForensicAnalysis => "Forensics",
            _ => "General"
        };
    }

    /// <summary>
    /// Converts session models to template format
    /// </summary>
    private Dictionary<string, ModelSelectionConfig> ConvertSessionModelsToTemplate(
        Dictionary<string, ModelConfiguration> sessionModels)
    {
        var templateModels = new Dictionary<string, ModelSelectionConfig>();

        foreach (var (capability, model) in sessionModels)
        {
            templateModels[capability] = new ModelSelectionConfig
            {
                ModelId = model.ModelId,
                Parameters = model.Parameters,
                MinMemoryRequired = model.MemoryUsage,
                AutoLoad = true
            };
        }

        return templateModels;
    }

    /// <summary>
    /// Converts session tools to template format
    /// </summary>
    private Dictionary<string, ToolConfig> ConvertSessionToolsToTemplate(List<string> enabledTools)
    {
        var tools = new Dictionary<string, ToolConfig>();

        foreach (var tool in enabledTools)
        {
            tools[tool] = new ToolConfig
            {
                Enabled = true,
                Settings = new Dictionary<string, object>()
            };
        }

        return tools;
    }

    /// <summary>
    /// Gets default system templates
    /// </summary>
    private List<ModelConfigurationTemplate> GetDefaultSystemTemplates()
    {
        return new List<ModelConfigurationTemplate>
        {
            // Fast Investigation Template - for quick responses
            new ModelConfigurationTemplate
            {
                Id = "system-fast-investigation",
                Name = "Fast Investigation",
                Description = "Optimized for quick responses with smaller, efficient models",
                Category = "Performance",
                Icon = "⚡",
                Tags = new() { "fast", "quick", "responsive", "low-memory" },
                Models = new()
                {
                    ["text"] = new ModelSelectionConfig
                    {
                        ModelId = "mistral:7b-instruct",
                        AlternativeModels = new() { "llama3.2:3b", "phi-3-mini" },
                        Parameters = new()
                        {
                            ["temperature"] = 0.7,
                            ["max_tokens"] = 2048,
                            ["top_p"] = 0.9
                        },
                        MinMemoryRequired = 8L * 1024 * 1024 * 1024 // 8GB
                    },
                    ["embedder"] = new ModelSelectionConfig
                    {
                        ModelId = "all-MiniLM-L6-v2",
                        Parameters = new()
                        {
                            ["normalize"] = true
                        },
                        MinMemoryRequired = 500L * 1024 * 1024 // 500MB
                    }
                },
                Performance = new()
                {
                    Priority = "Speed",
                    MaxConcurrentModels = 2,
                    MaxMemoryUsage = 16L * 1024 * 1024 * 1024 // 16GB
                }
            },

            // Deep Analysis Template - for comprehensive investigations
            new ModelConfigurationTemplate
            {
                Id = "system-deep-analysis",
                Name = "Deep Analysis",
                Description = "Maximum accuracy with large models for complex investigations",
                Category = "Quality",
                Icon = "🔬",
                Tags = new() { "accurate", "comprehensive", "detailed", "high-memory" },
                Models = new()
                {
                    ["text"] = new ModelSelectionConfig
                    {
                        ModelId = "llama3.1:70b",
                        AlternativeModels = new() { "mixtral:8x7b", "qwen:72b" },
                        Parameters = new()
                        {
                            ["temperature"] = 0.3,
                            ["max_tokens"] = 8192,
                            ["top_p"] = 0.95,
                            ["repeat_penalty"] = 1.1
                        },
                        MinMemoryRequired = 40L * 1024 * 1024 * 1024 // 40GB
                    }
                },
                Pipeline = new()
                {
                    Rag = new RagPipelineConfig
                    {
                        ChunkSize = 1024,
                        ChunkOverlap = 128,
                        TopK = 10,
                        UseReranking = true
                    }
                }
            },

            // Multi-Modal Investigation Template
            new ModelConfigurationTemplate
            {
                Id = "system-multimodal",
                Name = "Multi-Modal Investigation",
                Description = "Process text, images, and audio evidence together",
                Category = "Specialized",
                Icon = "🎭",
                Tags = new() { "multimodal", "images", "audio", "comprehensive" },
                Models = new()
                {
                    ["text"] = new ModelSelectionConfig
                    {
                        ModelId = "llava:34b",
                        AlternativeModels = new() { "bakllava:7b" },
                        Parameters = new()
                        {
                            ["temperature"] = 0.5,
                            ["max_tokens"] = 4096
                        },
                        MinMemoryRequired = 20L * 1024 * 1024 * 1024 // 20GB
                    },
                    ["vision"] = new ModelSelectionConfig
                    {
                        ModelId = "clip-vit-large",
                        Parameters = new()
                        {
                            ["batch_size"] = 32
                        },
                        MinMemoryRequired = 4L * 1024 * 1024 * 1024 // 4GB
                    },
                    ["audio"] = new ModelSelectionConfig
                    {
                        ModelId = "whisper-large-v3",
                        AlternativeModels = new() { "whisper-medium" },
                        Parameters = new()
                        {
                            ["language"] = "auto"
                        },
                        MinMemoryRequired = 3L * 1024 * 1024 * 1024 // 3GB
                    }
                },
                Pipeline = new()
                {
                    MultiModal = new MultiModalConfig
                    {
                        EnableCrossModalSearch = true,
                        AutoTranscribeAudio = true,
                        AutoAnalyzeImages = true
                    }
                }
            }
        };
    }
}