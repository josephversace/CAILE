# Run this in Package Manager Console or terminal in the IIM.Api project directory

# MediatR for command/query pattern (selective use)
dotnet add package MediatR
dotnet add package MediatR.Extensions.Microsoft.DependencyInjection

# FluentValidation for command validation
dotnet add package FluentValidation
dotnet add package FluentValidation.DependencyInjectionExtensions

# SignalR for real-time updates
# (SignalR is included in ASP.NET Core, no additional package needed)

# OpenIddict for OAuth2/OIDC
dotnet add package OpenIddict.AspNetCore
dotnet add package OpenIddict.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.Sqlite

# MinIO client
dotnet add package Minio

# Additional packages that might be needed
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package System.IdentityModel.Tokens.Jwt

Write-Host "NuGet packages installed successfully!" -ForegroundColor Green
