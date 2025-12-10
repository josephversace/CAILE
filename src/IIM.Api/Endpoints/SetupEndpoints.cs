// IIM.Api/Endpoints/SetupEndpoints.cs

using IIM.Application.Setup;
using IIM.Shared.Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace IIM.Api.Endpoints;

public static class SetupEndpoints
{
	public static void MapSetupEndpoints(this IEndpointRouteBuilder app)
	{
		var setup = app.MapGroup("/api/setup")
			.WithTags("Setup")
			.WithOpenApi();

		// POST /api/setup/seed
		setup.MapPost("/seed", async (
			[FromBody] SeedRequest request,
			[FromServices] IMediator mediator,
			CancellationToken ct) =>
		{
			try
			{
				var command = new SeedCommand
				{
					Token = request.Token,
					AdminEmail = request.AdminEmail,
					AdminPassword = request.AdminPassword
				};

				var result = await mediator.Send(command, ct);

				return result.Success
					? Results.Ok(result)
					: Results.BadRequest(result);
			}
			catch (UnauthorizedAccessException)
			{
				return Results.Unauthorized();
			}
		})
		.WithName("Seed")
		.WithSummary("Seed initial admin user and default data (one-time use)")
		.Produces<SeedResult>()
		.ProducesProblem(StatusCodes.Status400BadRequest)
		.ProducesProblem(StatusCodes.Status401Unauthorized);
	}
}

public record SeedRequest(string Token, string AdminEmail, string AdminPassword);
