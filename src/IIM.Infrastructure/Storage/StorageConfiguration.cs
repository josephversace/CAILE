using System;
using System.IO;
using System.Collections.Generic;

namespace IIM.Infrastructure.Storage;

    /// <summary>
    /// Central storage configuration for all IIM services.
    /// This is the single source of truth for all file system paths.
    /// </summary>
    public class StorageConfiguration
    {
        /// <summary>
        /// Gets or sets the base storage path for IIM data
        /// </summary>
        public string BasePath { get; set; } = GetDefaultBasePath();

        /// <summary>
        /// Gets or sets whether to use SQLite for structured data
        /// </summary>
        public bool UseSqlite { get; set; } = false;

        /// <summary>
        /// Gets or sets the SQLite database filename
        /// </summary>
        public string SqliteDbName { get; set; } = "iim.db";

        /// <summary>
        /// Gets the full path to the SQLite database
        /// </summary>
        public string SqlitePath => Path.Combine(BasePath, SqliteDbName);

        // ========================================
        // CORE DATA STORAGE
        // ========================================

        /// <summary>
        /// Gets the path for case storage
        /// </summary>
        public string CasesPath => Path.Combine(BasePath, "Cases");

        /// <summary>
        /// Gets the path for investigation sessions
        /// </summary>
        public string SessionsPath => Path.Combine(BasePath, "Sessions");

        /// <summary>
        /// Gets the path for evidence storage
        /// </summary>
        public string EvidencePath => Path.Combine(BasePath, "Evidence");

        /// <summary>
        /// Gets the path for vector storage (Qdrant data)
        /// </summary>
        public string VectorStorePath => Path.Combine(BasePath, "VectorStore");

        /// <summary>
        /// Gets the path for temporary files
        /// </summary>
        public string TempPath => Path.Combine(BasePath, "Temp");

        /// <summary>
        /// Gets the path for backup storage
        /// </summary>
        public string BackupPath => Path.Combine(BasePath, "Backups");

        /// <summary>
        /// Gets the path for logs
        /// </summary>
        public string LogsPath => Path.Combine(BasePath, "Logs");


    // ========================================
    // MinIO 
    // ========================================
    public string? MinioBucketName { get; set; } = "evidence";
    
    public bool VerifyHashOnUpload { get; set; } = true;


    // ========================================
    // MODEL STORAGE
    // ========================================

    /// <summary>
    /// Gets the root path for all AI models
    /// </summary>
    public string ModelsPath => Path.Combine(BasePath, "Models");

        /// <summary>
        /// Gets the path for system-provided models (read-only, managed by IIM)
        /// </summary>
        public string SystemModelsPath => Path.Combine(ModelsPath, "System");

        /// <summary>
        /// Gets the path for user-imported or custom models
        /// </summary>
        public string UserModelsPath => Path.Combine(ModelsPath, "User");

        /// <summary>
        /// Gets the path for downloaded models from model hub
        /// </summary>
        public string ModelCachePath => Path.Combine(ModelsPath, "Cache");

        /// <summary>
        /// Gets the path for fine-tuned models
        /// </summary>
        public string FineTunedModelsPath => Path.Combine(ModelsPath, "FineTuned");

        /// <summary>
        /// Gets the path for model metadata and configs
        /// </summary>
        public string ModelConfigsPath => Path.Combine(ModelsPath, "Configs");

        // ========================================
        // TEMPLATE STORAGE (Multiple Types)
        // ========================================

        /// <summary>
        /// Gets the root path for all templates
        /// </summary>
        public string TemplatesPath => Path.Combine(BasePath, "Templates");

        /// <summary>
        /// Gets the path for model configuration templates
        /// (Which models to use for different investigation types)
        /// </summary>
        public string ModelTemplatesPath => Path.Combine(TemplatesPath, "Models");

        /// <summary>
        /// Gets the path for investigation workflow templates
        /// (Predefined investigation workflows and methodologies)
        /// </summary>
        public string WorkflowTemplatesPath => Path.Combine(TemplatesPath, "Workflows");

        /// <summary>
        /// Gets the path for report templates
        /// (Export formats, layouts, styles)
        /// </summary>
        public string ReportTemplatesPath => Path.Combine(TemplatesPath, "Reports");

        /// <summary>
        /// Gets the path for prompt templates
        /// (Reusable prompts for different investigation types)
        /// </summary>
        public string PromptTemplatesPath => Path.Combine(TemplatesPath, "Prompts");

        /// <summary>
        /// Gets the path for tool configuration templates
        /// (Predefined tool settings for different scenarios)
        /// </summary>
        public string ToolTemplatesPath => Path.Combine(TemplatesPath, "Tools");

        /// <summary>
        /// Gets the path for case templates
        /// (Starter templates for different case types)
        /// </summary>
        public string CaseTemplatesPath => Path.Combine(TemplatesPath, "Cases");

        /// <summary>
        /// Gets the path for visualization templates
        /// (Chart configs, graph layouts, timeline styles)
        /// </summary>
        public string VisualizationTemplatesPath => Path.Combine(TemplatesPath, "Visualizations");

        /// <summary>
        /// Gets the path for query templates
        /// (Common investigation queries and searches)
        /// </summary>
        public string QueryTemplatesPath => Path.Combine(TemplatesPath, "Queries");

        // ========================================
        // PLUGIN STORAGE
        // ========================================

        /// <summary>
        /// Gets the root path for plugins
        /// </summary>
        public string PluginsPath => Path.Combine(BasePath, "Plugins");

        /// <summary>
        /// Gets the path for system plugins
        /// </summary>
        public string SystemPluginsPath => Path.Combine(PluginsPath, "System");

        /// <summary>
        /// Gets the path for user plugins
        /// </summary>
        public string UserPluginsPath => Path.Combine(PluginsPath, "User");

        /// <summary>
        /// Gets the path for plugin data storage
        /// </summary>
        public string PluginDataPath => Path.Combine(PluginsPath, "Data");

        // ========================================
        // CONFIGURATION STORAGE
        // ========================================

        /// <summary>
        /// Gets the path for application settings
        /// </summary>
        public string SettingsPath => Path.Combine(BasePath, "Settings");

        /// <summary>
        /// Gets the path for user preferences
        /// </summary>
        public string UserPreferencesPath => Path.Combine(SettingsPath, "UserPreferences.json");

        /// <summary>
        /// Gets the path for application configuration
        /// </summary>
        public string AppConfigPath => Path.Combine(SettingsPath, "AppConfig.json");

        /// <summary>
        /// Gets the path for security settings
        /// </summary>
        public string SecurityConfigPath => Path.Combine(SettingsPath, "Security.json");

        // ========================================
        // EXPORT STORAGE
        // ========================================

        /// <summary>
        /// Gets the path for exported files
        /// </summary>
        public string ExportsPath => Path.Combine(BasePath, "Exports");

        /// <summary>
        /// Gets the path for PDF exports
        /// </summary>
        public string PdfExportsPath => Path.Combine(ExportsPath, "PDF");

        /// <summary>
        /// Gets the path for Word exports
        /// </summary>
        public string WordExportsPath => Path.Combine(ExportsPath, "Word");

        /// <summary>
        /// Gets the path for Excel exports
        /// </summary>
        public string ExcelExportsPath => Path.Combine(ExportsPath, "Excel");

        /// <summary>
        /// Gets the path for JSON exports
        /// </summary>
        public string JsonExportsPath => Path.Combine(ExportsPath, "JSON");

        // ========================================
        // HELPER METHODS
        // ========================================

        /// <summary>
        /// Gets the default base path based on the operating system
        /// </summary>
        private static string GetDefaultBasePath()
        {
            // Check for environment variable override first
            var envPath = Environment.GetEnvironmentVariable("IIM_DATA_PATH");
            if (!string.IsNullOrEmpty(envPath))
            {
                return envPath;
            }

            // Use appropriate path for each OS
            if (OperatingSystem.IsWindows())
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "IIM"
                );
            }
            else if (OperatingSystem.IsMacOS())
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Library",
                    "Application Support",
                    "IIM"
                );
            }
            else // Linux and others
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".local",
                    "share",
                    "iim"
                );
            }
        }

        /// <summary>
        /// Ensures all required directories exist
        /// </summary>
        public void EnsureDirectoriesExist()
        {
            // Core directories
            Directory.CreateDirectory(BasePath);
            Directory.CreateDirectory(CasesPath);
            Directory.CreateDirectory(SessionsPath);
            Directory.CreateDirectory(EvidencePath);
            Directory.CreateDirectory(VectorStorePath);
            Directory.CreateDirectory(TempPath);
            Directory.CreateDirectory(BackupPath);
            Directory.CreateDirectory(LogsPath);

            // Model directories
            Directory.CreateDirectory(ModelsPath);
            Directory.CreateDirectory(SystemModelsPath);
            Directory.CreateDirectory(UserModelsPath);
            Directory.CreateDirectory(ModelCachePath);
            Directory.CreateDirectory(FineTunedModelsPath);
            Directory.CreateDirectory(ModelConfigsPath);

            // Template directories
            Directory.CreateDirectory(TemplatesPath);
            Directory.CreateDirectory(ModelTemplatesPath);
            Directory.CreateDirectory(WorkflowTemplatesPath);
            Directory.CreateDirectory(ReportTemplatesPath);
            Directory.CreateDirectory(PromptTemplatesPath);
            Directory.CreateDirectory(ToolTemplatesPath);
            Directory.CreateDirectory(CaseTemplatesPath);
            Directory.CreateDirectory(VisualizationTemplatesPath);
            Directory.CreateDirectory(QueryTemplatesPath);

            // Plugin directories
            Directory.CreateDirectory(PluginsPath);
            Directory.CreateDirectory(SystemPluginsPath);
            Directory.CreateDirectory(UserPluginsPath);
            Directory.CreateDirectory(PluginDataPath);

            // Settings directories
            Directory.CreateDirectory(SettingsPath);

            // Export directories
            Directory.CreateDirectory(ExportsPath);
            Directory.CreateDirectory(PdfExportsPath);
            Directory.CreateDirectory(WordExportsPath);
            Directory.CreateDirectory(ExcelExportsPath);
            Directory.CreateDirectory(JsonExportsPath);
        }

        /// <summary>
        /// Gets the full path for a model by ID
        /// </summary>
        public string GetModelPath(string modelId, ModelSource source = ModelSource.Auto)
        {
            return source switch
            {
                ModelSource.System => Path.Combine(SystemModelsPath, modelId),
                ModelSource.User => Path.Combine(UserModelsPath, modelId),
                ModelSource.Cache => Path.Combine(ModelCachePath, modelId),
                ModelSource.FineTuned => Path.Combine(FineTunedModelsPath, modelId),
                ModelSource.Auto => FindModelPath(modelId),
                _ => throw new ArgumentException($"Unknown model source: {source}")
            };
        }

        /// <summary>
        /// Finds a model path by searching all model directories
        /// </summary>
        private string FindModelPath(string modelId)
        {
            // Search order: FineTuned -> User -> Cache -> System
            var searchPaths = new[]
            {
                Path.Combine(FineTunedModelsPath, modelId),
                Path.Combine(UserModelsPath, modelId),
                Path.Combine(ModelCachePath, modelId),
                Path.Combine(SystemModelsPath, modelId)
            };

            foreach (var path in searchPaths)
            {
                if (Directory.Exists(path) || File.Exists(path))
                {
                    return path;
                }

                // Check for common model file extensions
                var extensions = new[] { ".gguf", ".bin", ".onnx", ".pt", ".safetensors" };
                foreach (var ext in extensions)
                {
                    var filePath = path + ext;
                    if (File.Exists(filePath))
                    {
                        return filePath;
                    }
                }
            }

            // Default to cache path for models that will be downloaded
            return Path.Combine(ModelCachePath, modelId);
        }

        /// <summary>
        /// Gets the appropriate template path based on template type
        /// </summary>
        public string GetTemplatePath(TemplateType type)
        {
            return type switch
            {
                TemplateType.Model => ModelTemplatesPath,
                TemplateType.Workflow => WorkflowTemplatesPath,
                TemplateType.Report => ReportTemplatesPath,
                TemplateType.Prompt => PromptTemplatesPath,
                TemplateType.Tool => ToolTemplatesPath,
                TemplateType.Case => CaseTemplatesPath,
                TemplateType.Visualization => VisualizationTemplatesPath,
                TemplateType.Query => QueryTemplatesPath,
                _ => TemplatesPath
            };
        }

        /// <summary>
        /// Gets storage statistics
        /// </summary>
        public StorageStatistics GetStatistics()
        {
            var stats = new StorageStatistics
            {
                TotalSize = GetDirectorySize(BasePath),
                ModelsSize = GetDirectorySize(ModelsPath),
                CasesSize = GetDirectorySize(CasesPath),
                EvidenceSize = GetDirectorySize(EvidencePath),
                VectorStoreSize = GetDirectorySize(VectorStorePath),
                TemplatesCount = CountFiles(TemplatesPath, "*.json"),
                ModelsCount = CountModels(),
                CasesCount = CountFiles(CasesPath, "*.json")
            };

            return stats;
        }

        /// <summary>
        /// Gets the size of a directory in bytes
        /// </summary>
        private long GetDirectorySize(string path)
        {
            if (!Directory.Exists(path))
                return 0;

            try
            {
                return new DirectoryInfo(path)
                    .GetFiles("*", SearchOption.AllDirectories)
                    .Sum(file => file.Length);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Counts files matching a pattern in a directory
        /// </summary>
        private int CountFiles(string path, string pattern)
        {
            if (!Directory.Exists(path))
                return 0;

            try
            {
                return Directory.GetFiles(path, pattern, SearchOption.AllDirectories).Length;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Counts the total number of models across all model directories
        /// </summary>
        private int CountModels()
        {
            var count = 0;
            var modelDirs = new[] { SystemModelsPath, UserModelsPath, ModelCachePath, FineTunedModelsPath };

            foreach (var dir in modelDirs)
            {
                if (Directory.Exists(dir))
                {
                    count += Directory.GetDirectories(dir).Length;
                }
            }

            return count;
        }

        /// <summary>
        /// Validates the storage configuration
        /// </summary>
        public ValidationResult Validate()
        {
            var result = new ValidationResult { IsValid = true };

            // Check base path accessibility
            try
            {
                var testFile = Path.Combine(BasePath, ".test");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Errors.Add($"Base path not writable: {ex.Message}");
            }

            // Check available disk space
            try
            {
                var drive = new DriveInfo(Path.GetPathRoot(BasePath) ?? "C:\\");
                var availableGB = drive.AvailableFreeSpace / (1024L * 1024 * 1024);

                if (availableGB < 10)
                {
                    result.Warnings.Add($"Low disk space: {availableGB}GB available");
                }

                if (availableGB < 1)
                {
                    result.IsValid = false;
                    result.Errors.Add($"Insufficient disk space: {availableGB}GB available");
                }
            }
            catch (Exception ex)
            {
                result.Warnings.Add($"Could not check disk space: {ex.Message}");
            }

            return result;
        }
    }

    /// <summary>
    /// Enum for model source locations
    /// </summary>
    public enum ModelSource
    {
        Auto,       // Search all locations
        System,     // System-provided models
        User,       // User-imported models
        Cache,      // Downloaded from hub
        FineTuned   // Fine-tuned models
    }

    /// <summary>
    /// Enum for template types
    /// </summary>
    public enum TemplateType
    {
        Model,          // Model configuration templates
        Workflow,       // Investigation workflow templates
        Report,         // Report export templates
        Prompt,         // Prompt templates
        Tool,           // Tool configuration templates
        Case,           // Case starter templates
        Visualization,  // Visualization templates
        Query          // Query templates
    }

    /// <summary>
    /// Storage statistics
    /// </summary>
    public class StorageStatistics
    {
        public long TotalSize { get; set; }
        public long ModelsSize { get; set; }
        public long CasesSize { get; set; }
        public long EvidenceSize { get; set; }
        public long VectorStoreSize { get; set; }
        public int TemplatesCount { get; set; }
        public int ModelsCount { get; set; }
        public int CasesCount { get; set; }

        /// <summary>
        /// Gets the total size in GB
        /// </summary>
        public double TotalSizeGB => TotalSize / (1024.0 * 1024.0 * 1024.0);

        /// <summary>
        /// Gets the models size in GB
        /// </summary>
        public double ModelsSizeGB => ModelsSize / (1024.0 * 1024.0 * 1024.0);
    }

    /// <summary>
    /// Validation result for storage configuration
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }
