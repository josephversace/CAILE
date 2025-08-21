using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Validation.AspNetCore;
using IIM.Api.Configuration;

namespace IIM.Api.Extensions
{
    public static class AuthenticationExtensions
    {
        public static IServiceCollection AddAuthenticationServices(
            this IServiceCollection services,
            IConfiguration configuration,
            DeploymentConfiguration deployment)
        {
            if (deployment.Mode == DeploymentMode.Server)
            {
                // Simplified OpenIddict setup - AuthDbContext needs to be created
                // For now, just use JWT authentication

                services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            ValidIssuer = configuration["Jwt:Issuer"],
                            ValidAudience = configuration["Jwt:Audience"],
                            IssuerSigningKey = new SymmetricSecurityKey(
                                System.Text.Encoding.UTF8.GetBytes(
                                    configuration["Jwt:Key"] ?? "default-development-key-change-in-production"))
                        };
                    });

                services.AddAuthorization();
            }
            else if (deployment.RequireAuth)
            {
                // Simple JWT for standalone with auth
                services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = false,
                            ValidateAudience = false,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            IssuerSigningKey = new SymmetricSecurityKey(
                                System.Text.Encoding.UTF8.GetBytes(
                                    configuration["Jwt:Key"] ?? "default-development-key-change-in-production"))
                        };
                    });

                services.AddAuthorization();
            }

            return services;
        }
    }
}