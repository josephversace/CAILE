using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IIM.Infrastructure.Foundry;

internal sealed class FoundryListResponse
{
	[JsonPropertyName("models")]
	public List<FoundryCatalogModel> Models { get; set; } = new();
}

internal sealed class FoundryCatalogModel
{
	[JsonPropertyName("id")]
	public string Id { get; set; } = string.Empty;

	[JsonPropertyName("alias")]
	public string? Alias { get; set; }

	[JsonPropertyName("name")]
	public string Name { get; set; } = string.Empty;

	[JsonPropertyName("display_name")]
	public string DisplayName { get; set; } = string.Empty;

	[JsonPropertyName("publisher")]
	public string? Publisher { get; set; }

	[JsonPropertyName("license")]
	public string? License { get; set; }

	[JsonPropertyName("license_description")]
	public string? LicenseDescription { get; set; }

	[JsonPropertyName("provider_type")]
	public string? ProviderType { get; set; }

	[JsonPropertyName("uri")]
	public string? Uri { get; set; }

	[JsonPropertyName("version")]
	public string? Version { get; set; }

	[JsonPropertyName("model_type")]
	public string? ModelType { get; set; }

	[JsonPropertyName("task")]
	public string? Task { get; set; }

	[JsonPropertyName("file_size_mb")]
	public double FileSizeMb { get; set; }

	[JsonPropertyName("supports_tool_calling")]
	public bool SupportsToolCalling { get; set; }

	[JsonPropertyName("parent_model_uri")]
	public string? ParentModelUri { get; set; }

	[JsonPropertyName("prompt_template")]
	public FoundryPromptTemplate? PromptTemplate { get; set; }

	[JsonPropertyName("runtime")]
	public FoundryRuntimeInfo? Runtime { get; set; }

	[JsonPropertyName("model_settings")]
	public FoundryModelSettings? ModelSettings { get; set; }
}

internal sealed class FoundryPromptTemplate
{
	[JsonPropertyName("assistant")]
	public string? Assistant { get; set; }

	[JsonPropertyName("prompt")]
	public string? Prompt { get; set; }

	[JsonPropertyName("system")]
	public string? System { get; set; }

	[JsonPropertyName("user")]
	public string? User { get; set; }
}

internal sealed class FoundryRuntimeInfo
{
	[JsonPropertyName("device_type")]
	public string? DeviceType { get; set; }

	[JsonPropertyName("execution_provider")]
	public string? ExecutionProvider { get; set; }
}

internal sealed class FoundryModelSettings
{
	[JsonPropertyName("parameters")]
	public List<FoundryParameter> Parameters { get; set; } = new();
}

internal sealed class FoundryParameter
{
	[JsonPropertyName("name")]
	public string? Name { get; set; }

	[JsonPropertyName("type")]
	public string? Type { get; set; }
}
