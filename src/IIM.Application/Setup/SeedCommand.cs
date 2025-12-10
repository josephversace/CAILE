using IIM.Shared.Mediator;

namespace IIM.Application.Setup;

public record SeedCommand : IRequest<SeedResult>
{
	public required string Token { get; init; }
	public required string AdminEmail { get; init; }
	public required string AdminPassword { get; init; }  // Plain password, Identity hashes it
}

public record SeedResult
{
	public bool Success { get; init; }
	public string? Message { get; init; }
	public string? Error { get; init; }
}