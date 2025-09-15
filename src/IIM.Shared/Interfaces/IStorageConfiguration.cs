using IIM.Shared.Models;

namespace IIM.Shared.Interfaces
{
    public interface IStorageConfiguration
    {
        string AppConfigPath { get; }
        string BackupPath { get; }
        string BasePath { get; set; }
        string CasesPath { get; }
        string CaseTemplatesPath { get; }
        string EvidencePath { get; }
        string ExcelExportsPath { get; }
        string ExportsPath { get; }
        string FineTunedModelsPath { get; }
        string JsonExportsPath { get; }
        string LogsPath { get; }
       
        string ModelCachePath { get; }
        string ModelConfigsPath { get; }
        string ModelsPath { get; }
        string ModelTemplatesPath { get; }
        string PdfExportsPath { get; }
        string PluginDataPath { get; }
        string PluginsPath { get; }
        string PromptTemplatesPath { get; }
        string QueryTemplatesPath { get; }
        string ReportTemplatesPath { get; }
        string SecurityConfigPath { get; }
        string SessionsPath { get; }
        string SettingsPath { get; }
        string SqliteDbName { get; set; }
        string SqlitePath { get; }
        string SystemModelsPath { get; }
        string SystemPluginsPath { get; }
        string TemplatesPath { get; }
        string TempPath { get; }
        string ToolTemplatesPath { get; }
        string UserModelsPath { get; }
        string UserPluginsPath { get; }
        string UserPreferencesPath { get; }
        bool UseSqlite { get; set; }
        string VectorStorePath { get; }
        bool VerifyHashOnUpload { get; set; }
        string VisualizationTemplatesPath { get; }
        string WordExportsPath { get; }
        string WorkflowTemplatesPath { get; }

        void EnsureDirectoriesExist();
        string GetModelPath(string modelId, ModelSource source = ModelSource.Auto);
        StorageStatistics GetStatistics();
        string GetTemplatePath(TemplateType type);
        ValidationResult Validate();
    }
}