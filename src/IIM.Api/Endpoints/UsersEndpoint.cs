using IIM.Application.Users;
using IIM.Shared.Interfaces;
using IIM.Shared.Mediator;
using IIM.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace IIM.Api.Endpoints
{
	public static class UserEndpoints
	{
		public static void MapUserEndpoints(this IEndpointRouteBuilder app)
		{
			var users = app.MapGroup("/api/users")
				.WithTags("Users")
				.WithOpenApi();

			// SEARCH USERS
			users.MapGet("/search", async (
				[FromQuery] string query,
				IMediator mediator,
				CancellationToken ct) =>
			{
				var result = await mediator.Send(new SearchUsersQuery(query), ct);
				return Results.Ok(result);
			})
			.WithName("SearchUsers")
			.WithSummary("Search users by name or email")
			.Produces<IEnumerable<ApplicationUser>>();
		}
	}
}
