// IIM.Shared/Models/Configuration/EffectivePromptDto.cs
namespace IIM.Shared.Models.Configuration;

public sealed class EffectivePrompt
{
	public PromptDefinition Definition { get; init; } = default!;
	public bool IsDefault { get; init; }
	public bool IsOverridden { get; init; }
}
