using IIM.Plugin.SDK;
using IIM.Shared.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Application.Plugins
{
    public class ForensicAnalysisPlugin : InvestigationPlugin
    {
        private readonly IWorkspaceManager _workspaceProvider;

        public override string Id => "com.example.forensicanalysis";
        public override string Name => "Forensic Analysis Plugin";
        public override string Description => "A sample plugin for forensic metadata analysis.";
        public override string Version => "1.0.0";
        public override PluginCapabilities Capabilities => new()
        {
            CanProcessText = true,
            CanProcessFiles = true
        };

        public ForensicAnalysisPlugin(IWorkspaceManager workspaceProvider)
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
                  	// Simple forensic metadata analysis example
                    if (virtualFile.ChainOfCustody == null || virtualFile.ChainOfCustody.Count == 0)
                        {
                            return new PluginResult { Success = false, Error = "No chain of custody information available for the file." };
						}

					
						// In a real plugin, you would perform complex analysis here.
						var analysisResult = $"Analysis of {virtualFile.FileName}:\n" +
                                             $"- Collected By: {virtualFile.StoredFileHash}\n" +
                                             $"- Collection Date: {virtualFile.ChainOfCustody[0].Timestamp}\n" +
                                             $"- Collection Location: {virtualFile.ChainOfCustody[0].Actor}";

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

