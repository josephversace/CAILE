using IIM.Plugin.SDK;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;

namespace IIM.Core.Plugins
{
    /// <summary>
    /// Plugin manifest with security permissions
    /// </summary>
    public class PluginManifest
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string? Description { get; set; }
        public PluginAuthor? Author { get; set; }
        public PluginPermissions? Permissions { get; set; }
    }

    /// <summary>
    /// Plugin author information
    /// </summary>
    public class PluginAuthor
    {
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Website { get; set; }
    }

    /// <summary>
    /// Plugin permission requirements
    /// </summary>
    public class PluginPermissions
    {
        public string NetworkAccess { get; set; } = "None";
        public string FileSystemAccess { get; set; } = "Sandboxed";
        public bool ProcessExecution { get; set; } = false;
        public List<string> RequiredAPIs { get; set; } = new();
    }
    /// <summary>
    /// Internal class to track loaded plugins
    /// </summary>
    public class LoadedPlugin
    {
        public required IInvestigationPlugin Plugin { get; init; }
        public required PluginManifest Manifest { get; init; }
        public required PluginContext Context { get; init; }
        public required string TempDirectory { get; init; }
        public required DateTime LoadedAt { get; init; }
    }


    /// <summary>
    /// Custom assembly load context for plugin isolation
    /// </summary>
    public class PluginLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;

        public PluginLoadContext(string pluginPath) : base(isCollectible: true)
        {
            _resolver = new AssemblyDependencyResolver(pluginPath);
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            return assemblyPath != null ? LoadFromAssemblyPath(assemblyPath) : null;
        }
    }

}
