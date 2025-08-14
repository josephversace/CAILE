using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IIM.Core.Plugins.Security;

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

/// <summary>
/// Result of plugin validation
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Whether the plugin passed validation
    /// </summary>
    public bool IsValid => !Errors.Any();
    
    /// <summary>
    /// List of validation errors
    /// </summary>
    public List<string> Errors { get; } = new();
    
    /// <summary>
    /// List of warnings (non-blocking)
    /// </summary>
    public List<string> Warnings { get; } = new();
    
    /// <summary>
    /// Add an error to the result
    /// </summary>
    public void AddError(string error) => Errors.Add(error);
    
    /// <summary>
    /// Add a warning to the result
    /// </summary>
    public void AddWarning(string warning) => Warnings.Add(warning);
}
