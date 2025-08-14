using System.Threading;
using System.Threading.Tasks;

namespace IIM.Plugin.SDK;

public interface IInvestigationPlugin
{
    string Id { get; }
    string Name { get; }
    string Description { get; }
    PluginCapabilities Capabilities { get; }

    Task InitializeAsync(PluginContext context);
    Task<PluginResult> ExecuteAsync(PluginRequest request, CancellationToken ct = default);
    Task<bool> ValidateAsync();
    Task DisposeAsync();
}
