using System.Text;
using IIM.Infrastructure.Data;
using IIM.Shared.Configuration;
using IIM.Shared.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace IIM.Api.Extensions;

public static class IdentityLayer
{
	public static IServiceCollection AddIdentityAndAuth(
		this IServiceCollection services,
		IConfiguration config,
		DeploymentConfiguration deployment)
	{
		services.AddIdentity<ApplicationUser, ApplicationRole>(opts =>
		{
			opts.Password.RequireDigit = true;
			opts.Password.RequireUppercase = true;
			opts.Password.RequireNonAlphanumeric = true;
		})
		.AddEntityFrameworkStores<AuthDbContext>()
		.AddDefaultTokenProviders();

		services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
			.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, jwt =>
			{
				jwt.TokenValidationParameters = new TokenValidationParameters
				{
					ValidIssuer = config["Jwt:Issuer"],
					ValidAudience = config["Jwt:Audience"],
					IssuerSigningKey = new SymmetricSecurityKey(
						Encoding.UTF8.GetBytes(config["Jwt:Key"]!)
					),
					ValidateIssuerSigningKey = true,
					ValidateLifetime = true,
					ValidateIssuer = deployment.Mode == DeploymentMode.ServerNode,
					ValidateAudience = deployment.Mode == DeploymentMode.ServerNode
				};
			});

		services.AddAuthorization(options =>
		{
			options.AddPolicy("AdminOnly", p => p.RequireRole("Administrator"));
			options.AddPolicy("CanUseAI", p => p.RequireClaim("ai_access", "true"));
		});

		return services;
	}
}
