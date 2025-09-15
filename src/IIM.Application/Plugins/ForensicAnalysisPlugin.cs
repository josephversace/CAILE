using IIM.Plugin.SDK;
using IIM.Shared.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Application.Plugins
{
    public class ForensicAnalysisPlugin : InvestigationPlugin
    {
        private readonly IWorkspaceProvider _workspaceProvider;

        public override string Id => "com.example.forensicanalysis";
        public override string Name => "Forensic Analysis Plugin";
        public override string Description => "A sample plugin for forensic metadata analysis.";
        public override string Version => "1.0.0";
        public override PluginCapabilities Capabilities => new()
        {
            CanProcessText = true,
            CanProcessFiles = true
        };

        public ForensicAnalysisPlugin(IWorkspaceProvider workspaceProvider)
        {
            _workspaceProvider = workspaceProvider; // Injected via DI
        }

        public override async Task<PluginResult> ExecuteAsync(PluginRequest request, CancellationToken cancellationToken)
        {
            if (request.FunctionName == "analyze_file_metadata")
            {
                if (request.Parameters.TryGetValue("fileId", out var fileIdObj) && Guid.TryParse(fileIdObj.ToString(), out var fileId))
                {
                    var virtualFile = await _workspaceProvider.GetVirtualFileByIdAsync(fileId, cancellationToken);
                    if (virtualFile != null)
                    {
                        // In a real plugin, you would perform complex analysis here.
                        var analysisResult = $"Analysis of {virtualFile.FileName}:\n" +
                                             $"- Collected By: {virtualFile.CollectedBy}\n" +
                                             $"- Collection Date: {virtualFile.CollectionDate}\n" +
                                             $"- Collection Location: {virtualFile.CollectedLocation}";

                        return new PluginResult { Success = true, Message = "Analysis complete.", Data = analysisResult };
                    }
                    return new PluginResult { Success = false, Error = "File not found." };
                }
                return new PluginResult { Success = false, Error = "Invalid or missing 'fileId' parameter." };
            }
            return new PluginResult { Success = false, Error = $"Unknown function: {request.FunctionName}" };
        }
    }
}

