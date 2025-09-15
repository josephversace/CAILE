using IIM.Shared.Models.Core;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Plugin.SDK
{
    public abstract class InvestigationPlugin
    {
        public abstract string Id { get; }
        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract string Version { get; }
        public abstract PluginCapabilities Capabilities { get; }

        public PluginContext Context { get; private set; }

        public void Initialize(PluginContext context)
        {
            Context = context;
        }

        public abstract Task<PluginResult> ExecuteAsync(PluginRequest request, CancellationToken cancellationToken);
    }
}

