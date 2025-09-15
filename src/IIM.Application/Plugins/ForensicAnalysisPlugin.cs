using IIM.Plugin.SDK;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using IIM.Shared.Models.Core;

namespace IIM.Application.Plugins
{
    public class ForensicAnalysisPlugin : InvestigationPlugin
    {
        private readonly ILogger<ForensicAnalysisPlugin> _logger;
        private readonly IWorkspaceProvider _workspaceProvider;

        public ForensicAnalysisPlugin(ILogger<ForensicAnalysisPlugin> logger, IWorkspaceProvider workspaceProvider)
        {
            _logger = logger;
            _workspaceProvider = workspaceProvider;
        }

        public override string Name => "Forensic Meta-Data Analyzer";
        public override string Description => "Analyzes forensic metadata of files, such as collection dates and locations.";
        public override string Version => "1.0";

        public override async Task<PluginResult> ExecuteAsync(PluginRequest request)
        {
            if (!request.Parameters.TryGetValue("fileId", out var fileIdObj) || !Guid.TryParse(fileIdObj.ToString(), out var fileId))
            {
                return new PluginResult { Success = false, ErrorMessage = "Invalid or missing 'fileId' parameter." };
            }

            _logger.LogInformation("ForensicAnalysisPlugin started for file: {FileId}", fileId);

            try
            {
                var virtualFile = await _workspaceProvider.GetVirtualFileByIdAsync(fileId);
                if (virtualFile == null)
                {
                    return new PluginResult { Success = false, ErrorMessage = $"File with ID '{fileId}' not found." };
                }

                // Now we can safely access the forensic data from the VirtualFile object
                var analysisResult = new
                {
                    CollectionDate = virtualFile.CollectionDate,
                    CollectedBy = virtualFile.CollectedBy,
                    CollectionLocation = virtualFile.CollectedLocation,
                    CustomMetadata = virtualFile.CustomMetadata,
                    Hash = virtualFile.StoredFileHash
                };

                var result = new PluginResult
                {
                    Success = true,
                    Message = "Forensic metadata analysis complete.",
                    Data = analysisResult,
                    // You could also add a visualization here if desired
                    // VisualizationType = "Table", 
                };

                _logger.LogInformation("ForensicAnalysisPlugin completed for file: {FileId}", fileId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing ForensicAnalysisPlugin for file {FileId}", fileId);
                return new PluginResult
                {
                    Success = false,
                    ErrorMessage = $"An unexpected error occurred: {ex.Message}"
                };
            }
        }
    }
}
