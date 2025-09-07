using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IIM.Shared.Models;

/// <summary>
/// Represents the JSON object for a single service returned by 'docker-compose ps --format json'.
/// </summary>
public class DockerComposeServiceState
{
    [JsonPropertyName("Name")]
    public string Name { get; set; }

    [JsonPropertyName("State")]
    public string State { get; set; } // e.g., "running", "exited"

    [JsonPropertyName("Health")]
    public string Health { get; set; } // e.g., "healthy", "unhealthy", ""

    [JsonPropertyName("ExitCode")]
    public int ExitCode { get; set; }
}
