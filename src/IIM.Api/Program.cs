using IIM.Api.Configuration;
using IIM.Api.Endpoints;
using IIM.Api.Extensions;
using IIM.Api.Hubs;
using IIM.Application.Commands.Evidence;
using IIM.Application.Commands.Investigation;
using IIM.Application.Commands.Models;        
using IIM.Core.AI;
using IIM.Core.Inference;
using IIM.Core.Mediator;
using IIM.Core.Models;                       
using IIM.Core.Services;
using IIM.Infrastructure.Platform;

using IIM.Shared.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.OpenApi.Models;
using System.Text.Json;


var builder = WebApplication.CreateBuilder(args);

// ============================================
// Load deployment configuration
// ============================================
var deploymentConfig = new DeploymentConfiguration();
builder.Configuration.GetSection("Deployment").Bind(deploymentConfig);

// ============================================
// Add services using extension methods
// ============================================
builder.Services.AddApiServices(builder.Configuration);


builder.Services.AddEndpointsApiExplorer(); // Required for minimal APIs
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "IIM API",
        Version = "v1",
        Description = "Intelligent Investigation Machine API",
        Contact = new OpenApiContact
        {
            Name = "IIM Team",
            Email = "support@iim.local"
        }
    });
});



builder.Services.AddHealthChecks();

// Add response compression for SignalR
builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" });
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazor", policy =>
    {
        policy.WithOrigins("http://localhost:5000", "http://localhost:5001")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // Required for SignalR
    });
});

var app = builder.Build();

// ============================================
// Configure pipeline
// ============================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "IIM API v1");
        options.RoutePrefix = "swagger"; // Swagger at /swagger

        // Optional: Make Swagger the default page
        // options.RoutePrefix = string.Empty; // Swagger at root
    });
}

app.UseResponseCompression();
app.UseCors("AllowBlazor");

// Add authentication if required
if (deploymentConfig.RequireAuth)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

// Health checks
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});

// SignalR hubs
app.MapHub<InvestigationHub>("/hubs/investigation");
if (deploymentConfig.Mode == DeploymentMode.Server)
{
    app.MapHub<AdminHub>("/hubs/admin");
    app.MapRazorPages(); // Admin pages
}


// ============================================
// Map all endpoints from separate files
// ============================================
app.MapSystemEndpoints();        // System & health endpoints
app.MapInvestigationEndpoints(); // Investigation endpoints
app.MapEvidenceEndpoints();      // Evidence management
app.MapModelEndpoints();         // Model management
app.MapWslEndpoints();           // WSL management
app.MapInferenceEndpoints();     // Inference & generation
app.MapRagEndpoints();           // RAG endpoints
app.MapAuditEndpoints();         // Audit logging
app.MapCaseEndpoints();

// Start the application
app.Run("http://localhost:5080");

