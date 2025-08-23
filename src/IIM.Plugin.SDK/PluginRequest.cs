using System;
using System.Collections.Generic;
using IIM.Shared.DTOs;

namespace IIM.Plugin.SDK;

/// <summary>
/// Request sent to a plugin for execution
/// </summary>
public class PluginRequest
{
    /// <summary>
    /// The intent extracted from user query (e.g., "analyze_hash")
    /// </summary>
    public required string Intent { get; set; }
    
    /// <summary>
    /// Parameters extracted from the query or provided by the system
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
    
    /// <summary>
    /// Current case identifier for evidence association
    /// </summary>
    public string? CaseId { get; set; }
    
    /// <summary>
    /// User making the request for audit purposes
    /// </summary>
    public string? UserId { get; set; }
    
    /// <summary>
    /// Evidence context if applicable
    /// </summary>
    public EvidenceContext? Evidence { get; set; }
    
    /// <summary>
    /// Original user query that triggered this plugin
    /// </summary>
    public string? OriginalQuery { get; set; }
    
    /// <summary>
    /// Tags for categorization and routing
    /// </summary>
    public HashSet<string> Tags { get; set; } = new();
}

