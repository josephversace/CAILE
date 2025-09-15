using IIM.Shared.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace IIM.Plugin.SDK
{
    public class PluginContext
    {
        public ILogger Logger { get; }
        public IWorkspaceProvider WorkspaceProvider { get; }
        public HttpClient HttpClient { get; }

        public PluginContext(ILogger logger, IWorkspaceProvider workspaceProvider, HttpClient httpClient)
        {
            Logger = logger;
            WorkspaceProvider = workspaceProvider;
            HttpClient = httpClient;
        }
    }
}

