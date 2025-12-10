using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using System.Security.Claims;

namespace IIM.Api.Services;

public class FilePermissionHandler : AuthorizationHandler<OperationAuthorizationRequirement>
{
	protected override Task HandleRequirementAsync(
		AuthorizationHandlerContext context,
		OperationAuthorizationRequirement requirement)
	{
		var claims = context.User.Claims
			.Where(c => c.Type.StartsWith("perm:"))
			.ToDictionary(c => c.Type.Replace("perm:", ""), c => c.Value);

		if (!claims.TryGetValue(requirement.Name, out var raw))
			return Task.CompletedTask;                // no permission -> forbidden

		var allowed = raw.Split(",");                 // Read|Write|Delete
		if (allowed.Contains(requirement.Name))
			context.Succeed(requirement);

		return Task.CompletedTask;
	}
}
