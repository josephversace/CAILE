using IIM.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace IIM.Core.Plugins;

/// <summary>
/// Validates plugins before loading for security and compatibility
/// </summary>
public interface IPluginValidator
{
    /// <summary>
    /// Validate a plugin package
    /// </summary>
    Task<ValidationResult> ValidateAsync(string pluginPath);
}
