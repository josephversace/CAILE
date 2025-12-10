using System.Security.Claims;

namespace IIM.Shared.Extensions
{
	public static class HttpContextExtensions
	{
		public static string? GetUserIdString(this ClaimsPrincipal user)
		{
			return user.FindFirst("sub")?.Value
				?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		}
	}
}
