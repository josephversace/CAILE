using IIM.Shared.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace IIM.Plugin.SDK
{
    public class PluginContext
    {
        public ILogger Logger { get; }
        public IWorkspaceManager WorkspaceProvider { get; }
        public HttpClient HttpClient { get; }

        public PluginContext(ILogger logger, IWorkspaceManager workspaceProvider, HttpClient httpClient)
        {
            Logger = logger;
            WorkspaceProvider = workspaceProvider;
            HttpClient = httpClient;
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                // Cleanup any resources
                if (HttpClient != null)
                {
                    HttpClient.Dispose();
                }

                // Add other cleanup as needed
                GC.SuppressFinalize(this);
            }
            catch (Exception ex)
            {
                // Log but don't throw during disposal
                Logger?.LogError(ex, "Error during plugin context disposal");
            }
        }
    }
}

