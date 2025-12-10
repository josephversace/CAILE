using IIM.Shared.Models;
using Microsoft.AspNetCore.Identity;

public static class IdentitySeeder
{
	public static async Task SeedAsync(IServiceProvider services)
	{
		using var scope = services.CreateScope();
		var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
		var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

		const string adminRoleName = "Administrator";
		const string adminUserName = "admin";
		const string adminEmail = "admin@example.com";
		const string adminPassword = "Admin!123"; // change in prod

		// 1. Ensure role exists
		if (!await roleManager.RoleExistsAsync(adminRoleName))
		{
			await roleManager.CreateAsync(new ApplicationRole
			{
				Name = adminRoleName,
				NormalizedName = adminRoleName.ToUpper()
			});
		}

		// 2. Ensure user exists
		var adminUser = await userManager.FindByNameAsync(adminUserName);
		if (adminUser == null)
		{
			adminUser = new ApplicationUser
			{
				UserName = adminUserName,
				NormalizedUserName = adminUserName.ToUpper(),
				Email = adminEmail,
				NormalizedEmail = adminEmail.ToUpper(),
				EmailConfirmed = true,
				IsActive = true,
				RequireMfa = false,
				Organization = "System"
			};

			var result = await userManager.CreateAsync(adminUser, adminPassword);
			if (!result.Succeeded)
				throw new Exception(string.Join(";", result.Errors.Select(e => e.Description)));
		}

		// 3. Ensure user is in sa role
		if (!await userManager.IsInRoleAsync(adminUser, adminRoleName))
		{
			await userManager.AddToRoleAsync(adminUser, adminRoleName);
		}
	}
}
