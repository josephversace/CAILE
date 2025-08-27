
using IIM.Plugin.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Core.Plugins
{
    /// <summary>
    /// Interface for creating sandboxed plugin execution contexts
    /// </summary>
    public interface IPluginSandbox
    {
        /// <summary>
        /// Create a sandboxed context for a plugin
        /// </summary>
        Task<PluginContext> CreateContextAsync(PluginManifest plugin);

        /// <summary>
        /// Destroy a plugin context and clean up resources
        /// </summary>
        Task DestroyContextAsync(string pluginId);

        /// <summary>
        /// Validate plugin security requirements
        /// </summary>
        Task<bool> ValidateSecurityAsync(PluginManifest plugin);
    }
}
