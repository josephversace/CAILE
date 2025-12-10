using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IIM.Infrastructure.Data;
using IIM.Shared.Models;
using IIM.Shared.Models.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using QRCoder;

namespace IIM.Api.Endpoints
{
	public static class AuthEndpoints
	{
		public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
		{
			var auth = app.MapGroup("/api/auth")
						  .WithTags("Authentication")
						  .WithOpenApi();

			// ========================================================
			//  REGISTER USER
			// ========================================================
			auth.MapPost("/register", async (
				RegisterRequest req,
				UserManager<ApplicationUser> users) =>
			{
				var user = new ApplicationUser { UserName = req.Email, Email = req.Email };
				var result = await users.CreateAsync(user, req.Password);

				return result.Succeeded
					? Results.Ok("User created.")
					: Results.BadRequest(result.Errors);
			});

			// ========================================================
			//  JWT LOGIN (MAUI / external clients)
			// ========================================================
			auth.MapPost("/login", async (
				LoginRequest req,
				UserManager<ApplicationUser> users,
				IConfiguration config,
				IServiceProvider sp) =>
			{
				var user = await users.FindByEmailAsync(req.Email);
				if (user is null || !await users.CheckPasswordAsync(user, req.Password))
					return Results.Unauthorized();

				// MFA challenge first
				if (await users.GetTwoFactorEnabledAsync(user))
				{
					return Results.Ok(new
					{
						mfaRequired = true,
						userId = user.Id
					});
				}

				return Results.Ok(new
				{
					token = GenerateJwt(user, users, config, sp)
				});
			});

			// ========================================================
			//  JWT MFA LOGIN
			// ========================================================
			auth.MapPost("/login/mfa", async (
				MfaLoginRequest req,
				SignInManager<ApplicationUser> signIn,
				UserManager<ApplicationUser> users,
				IConfiguration config,
				IServiceProvider sp) =>
			{
				var user = await users.FindByIdAsync(req.UserId);
				if (user is null) return Results.Unauthorized();

				bool valid = await users.VerifyTwoFactorTokenAsync(
					user,
					TokenOptions.DefaultAuthenticatorProvider,
					req.Code);

				if (!valid) return Results.Unauthorized();

				// No cookie for MAUI
				return Results.Ok(new
				{
					token = GenerateJwt(user, users, config, sp)
				});
			});

			// ========================================================
			//  BFF COOKIE LOGIN (BLAZOR)
			// ========================================================
			auth.MapPost("/bff/login", async (
				LoginRequest req,
				SignInManager<ApplicationUser> signIn,
				UserManager<ApplicationUser> users) =>
			{
				var user = await users.FindByEmailAsync(req.Email);
				if (user is null || !await users.CheckPasswordAsync(user, req.Password))
					return Results.Unauthorized();

				if (await users.GetTwoFactorEnabledAsync(user))
				{
					return Results.Ok(new
					{
						mfaRequired = true,
						userId = user.Id
					});
				}

				await signIn.SignInAsync(user, isPersistent: true);

				return Results.Ok(new { success = true });
			});

			// ========================================================
			//  BFF COOKIE MFA LOGIN
			// ========================================================
			auth.MapPost("/bff/login/mfa", async (
				MfaLoginRequest req,
				SignInManager<ApplicationUser> signIn,
				UserManager<ApplicationUser> users) =>
			{
				var user = await users.FindByIdAsync(req.UserId);
				if (user is null) return Results.Unauthorized();

				bool valid = await users.VerifyTwoFactorTokenAsync(
					user,
					TokenOptions.DefaultAuthenticatorProvider,
					req.Code);

				if (!valid) return Results.Unauthorized();

				await signIn.SignInAsync(user, isPersistent: true);

				return Results.Ok(new { success = true });
			});

			// ========================================================
			//  BFF LOGOUT
			// ========================================================
			auth.MapPost("/bff/logout", async (SignInManager<ApplicationUser> signIn) =>
			{
				await signIn.SignOutAsync();
				return Results.Ok(new { success = true });
			});

			// ========================================================
			//  MFA MANAGEMENT (requires authorization)
			// ========================================================
			var mfa = auth.MapGroup("/mfa").RequireAuthorization();

			mfa.MapPost("/enable", async (
				HttpContext ctx,
				UserManager<ApplicationUser> users) =>
			{
				var user = await users.GetUserAsync(ctx.User);
				if (user is null) return Results.Unauthorized();

				await users.SetTwoFactorEnabledAsync(user, true);

				var key = await users.GetAuthenticatorKeyAsync(user);
				if (string.IsNullOrWhiteSpace(key))
				{
					await users.ResetAuthenticatorKeyAsync(user);
					key = await users.GetAuthenticatorKeyAsync(user);
				}

				var qrText = $"otpauth://totp/IIM:{user.Email}?secret={key}&issuer=IIM";

				using var gen = new QRCodeGenerator();
				using var qr = gen.CreateQrCode(qrText, QRCodeGenerator.ECCLevel.Q);
				using var img = new QRCode(qr).GetGraphic(20);

				using var ms = new MemoryStream();
				img.Save(ms, System.Drawing.Imaging.ImageFormat.Png);

				return Results.File(ms.ToArray(), "image/png");
			});

			mfa.MapPost("/verify", async (
				MfaVerifyRequest req,
				HttpContext ctx,
				UserManager<ApplicationUser> users) =>
			{
				var user = await users.GetUserAsync(ctx.User);
				if (user is null) return Results.Unauthorized();

				bool ok = await users.VerifyTwoFactorTokenAsync(
					user,
					TokenOptions.DefaultAuthenticatorProvider,
					req.Code);

				return ok ? Results.Ok("Verified") : Results.BadRequest("Invalid MFA code");
			});

			mfa.MapPost("/recovery-codes", async (
				HttpContext ctx,
				UserManager<ApplicationUser> users) =>
			{
				var user = await users.GetUserAsync(ctx.User);
				if (user is null) return Results.Unauthorized();

				var codes = await users.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
				return Results.Ok(codes);
			});

			// ========================================================
			//  ROLES
			// ========================================================
			auth.MapPost("/roles", async (
				string role,
				RoleManager<ApplicationRole> roleMgr) =>
			{
				if (await roleMgr.RoleExistsAsync(role))
					return Results.BadRequest("Role already exists");

				var result = await roleMgr.CreateAsync(new ApplicationRole { Name = role });
				return result.Succeeded ? Results.Ok("Role created") : Results.BadRequest("Failed");
			})
			.RequireAuthorization("Admin");

			auth.MapGet("/roles", (RoleManager<ApplicationRole> roleMgr) =>
			{
				return Results.Ok(roleMgr.Roles.Select(r => r.Name));
			})
			.RequireAuthorization("Admin");

			// ========================================================
			//  ASSIGN USER ROLES
			// ========================================================
			auth.MapPost("/users/{id}/roles/{role}", async (
				string id,
				string role,
				UserManager<ApplicationUser> userMgr,
				RoleManager<ApplicationRole> roleMgr) =>
			{
				var user = await userMgr.FindByIdAsync(id);
				if (user is null) return Results.NotFound("User not found");

				if (!await roleMgr.RoleExistsAsync(role))
					return Results.NotFound("Role missing");

				var result = await userMgr.AddToRoleAsync(user, role);
				return result.Succeeded ? Results.Ok("Assigned") : Results.BadRequest(result.Errors);
			})
			.RequireAuthorization("Admin");

			auth.MapGet("/users/{id}/roles", async (
				string id,
				UserManager<ApplicationUser> userMgr) =>
			{
				var user = await userMgr.FindByIdAsync(id);
				if (user is null) return Results.NotFound();

				return Results.Ok(await userMgr.GetRolesAsync(user));
			})
			.RequireAuthorization("Admin");
		}

		// ========================================================
		//  JWT GENERATION (with roles + file permissions)
		// ========================================================
		private static string GenerateJwt(
			ApplicationUser user,
			UserManager<ApplicationUser> users,
			IConfiguration config,
			IServiceProvider sp)
		{
			var claims = new List<Claim>
			{
				new Claim(ClaimTypes.NameIdentifier, user.Id),
				new Claim(ClaimTypes.Name, user.UserName!),
				new Claim(ClaimTypes.Email, user.Email!)
			};

			var roles = users.GetRolesAsync(user).Result;
			claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

			// Permissions from GovernanceDB
			using var scope = sp.CreateScope();
			var db = scope.ServiceProvider.GetRequiredService<GovernanceDbContext>();

			var permissions =
				from rule in db.AccessControlRules
				where roles.Contains(rule.AccessRole.Name)
				select new
				{
					rule.ClassificationTag.Name,
					rule.Permissions
				};

			foreach (var p in permissions)
				claims.Add(new Claim($"perm:{p.Name}", p.Permissions.ToString()));

			var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));

			var jwt = new JwtSecurityToken(
				issuer: config["Jwt:Issuer"],
				audience: config["Jwt:Audience"],
				claims: claims,
				expires: DateTime.UtcNow.AddHours(6),
				signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
			);

			return new JwtSecurityTokenHandler().WriteToken(jwt);
		}
	}
}
