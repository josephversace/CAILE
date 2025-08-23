using IIM.Api.Configuration;
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
using IIM.Shared.DTOs;
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
// API Endpoints - organized by route groups
// ============================================
var api = app.MapGroup("/api");

// System Status Endpoints (keep simple, no commands)
api.MapGet("/healthz", () => Results.Text("ok"))
    .WithName("HealthCheck")
    .WithOpenApi();

api.MapGet("/v1/gpu", async (IModelOrchestrator orchestrator) =>
{
    var stats = await orchestrator.GetStatsAsync();
    return Results.Json(new
    {
        vendor = "AMD/NVIDIA/CPU",
        provider = "directml",
        vramGb = 128,
        availableMemoryGb = stats.AvailableMemory / 1_073_741_824,
        usedMemoryGb = stats.TotalMemoryUsage / 1_073_741_824,
        loadedModels = stats.LoadedModels,
        models = stats.Models
    });
})
.WithName("GetGpuInfo")
.WithOpenApi();

api.MapGet("/v1/stats", async (IInferencePipeline pipeline, IModelOrchestrator orchestrator) =>
{
    var pipelineStats = pipeline.GetStats();
    var orchestratorStats = await orchestrator.GetStatsAsync();
    return Results.Json(new
    {
        pipeline = pipelineStats,
        orchestrator = orchestratorStats,
        timestamp = DateTimeOffset.UtcNow
    });
})
.WithName("GetSystemStats")
.WithOpenApi();

// ============================================
// Investigation Endpoints (use SimpleMediator for audit)
// ============================================
var investigation = api.MapGroup("/investigation");

investigation.MapPost("/session", async (
   [FromServices] IMediator mediator,
    CreateSessionCommand command) =>
{
    var result = await mediator.Send(command);
    return Results.Ok(result);
})
.WithName("CreateSession")
.RequireAuthorization();

investigation.MapPost("/query", async (
    [FromServices] IMediator mediator,
    ProcessInvestigationCommand command) =>
{
    var result = await mediator.Send(command);
    return Results.Ok(result);
})
.WithName("ProcessQuery")
.RequireAuthorization();

// ============================================
// Evidence Endpoints (use SimpleMediator for chain of custody)
// ============================================
var evidence = api.MapGroup("/evidence");

evidence.MapPost("/ingest", async (
    HttpRequest request,
    [FromServices] IMediator mediator,
    ILogger<Program> logger) =>
{
    if (!request.HasFormContentType)
    {
        return Results.BadRequest("Form data required");
    }

    try
    {
        var form = await request.ReadFormAsync();
        var file = form.Files.FirstOrDefault();

        if (file == null || file.Length == 0)
        {
            return Results.BadRequest("No file provided");
        }

        // Parse metadata from form
        var metadata = new EvidenceMetadata
        {
            CaseNumber = form["caseNumber"].ToString(),
            CollectedBy = form["collectedBy"].ToString(),
            CollectionDate = DateTimeOffset.TryParse(form["collectionDate"], out var date)
                ? date : DateTimeOffset.UtcNow,
            CollectionLocation = form["collectionLocation"].ToString(),
            DeviceSource = form["deviceSource"].ToString(),
            Description = form["description"].ToString()
        };

        // Create command for mediator (audit trail)
        using var stream = file.OpenReadStream();
        var command = new IngestEvidenceCommand
        {
            FileStream = stream,
            FileName = file.FileName,
            Metadata = metadata
        };

        var result = await mediator.Send(command);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to ingest evidence");
        return Results.Problem(
            detail: ex.Message,
            statusCode: 500,
            title: "Evidence Ingestion Failed");
    }
})
.WithName("IngestEvidence")
.RequireAuthorization()
.DisableAntiforgery(); // Required for file uploads

evidence.MapPost("/{evidenceId}/verify", async (
    string evidenceId,
    IEvidenceManager evidenceManager) =>
{
    var isValid = await evidenceManager.VerifyIntegrityAsync(evidenceId);
    return Results.Ok(new
    {
        evidenceId,
        integrityValid = isValid,
        timestamp = DateTimeOffset.UtcNow,
        message = isValid ? "Integrity verified" : "Integrity check failed"
    });
})
.WithName("VerifyEvidence")
.RequireAuthorization();

evidence.MapGet("/{evidenceId}/chain", async (
    string evidenceId,
    IEvidenceManager evidenceManager) =>
{
    var report = await evidenceManager.GenerateChainOfCustodyAsync(evidenceId);
    return Results.Ok(report);
})
.WithName("GetChainOfCustody")
.RequireAuthorization();

// ============================================
// Model Management Endpoints (use commands for resource tracking)
// ============================================
var models = api.MapGroup("/v1/models");

models.MapPost("/load", async (
    [FromServices] IMediator mediator,
    LoadModelCommand command) =>
{
    var result = await mediator.Send(command);
    return Results.Ok(result);
})
.WithName("LoadModel")
.RequireAuthorization();

models.MapPost("/{modelId}/unload", async (
    string modelId,
    [FromServices] IMediator mediator) =>
{
    var command = new UnloadModelCommand { ModelId = modelId };
    var result = await mediator.Send(command);
    return Results.Ok(result);
})
.WithName("UnloadModel")
.RequireAuthorization();

// ============================================
// Inference Endpoints (direct service calls, no audit needed)
// ============================================
api.MapPost("/v1/generate", async (
    [FromBody] GenerateRequest request,
    IInferencePipeline pipeline,
    ILogger<Program> logger) =>
{
    try
    {
        var pipelineRequest = new InferencePipelineRequest
        {
            ModelId = request.ModelId,
            Input = request.Prompt,
            Parameters = request.Parameters,
            Tags = request.Tags
        };

        var result = await pipeline.ExecuteAsync<string>(pipelineRequest);
        return Results.Json(new
        {
            text = result,
            modelId = request.ModelId,
            timestamp = DateTimeOffset.UtcNow
        });
    }
    catch (ModelNotLoadedException ex)
    {
        return Results.Problem(
            detail: ex.Message,
            statusCode: 404,
            title: "Model Not Found");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Generation failed");
        return Results.Problem(
            detail: ex.Message,
            statusCode: 500,
            title: "Generation Failed");
    }
})
.WithName("Generate")
.WithOpenApi();

// ============================================
// WSL Management Endpoints (direct service calls)
// ============================================
var wsl = api.MapGroup("/wsl");

wsl.MapGet("/status", async (IWslManager wslManager) =>
{
    var status = await wslManager.GetStatusAsync();
    return Results.Ok(status);
})
.WithName("GetWslStatus")
.WithOpenApi();

wsl.MapGet("/health", async (IWslManager wslManager) =>
{
    var health = await wslManager.HealthCheckAsync();
    return health.IsHealthy ? Results.Ok(health) : Results.Problem(
        detail: string.Join("; ", health.Issues),
        statusCode: 503,
        title: "WSL Unhealthy");
})
.WithName("GetWslHealth")
.WithOpenApi();

wsl.MapPost("/ensure", async (IWslManager wslManager) =>
{
    try { 
    var distro = await wslManager.EnsureDistroAsync();
    return Results.Ok(distro);
}
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.Message,
            statusCode: 500,
            title: "WSL Distro Setup Failed");
    }
})
.WithName("EnsureWslDistro")
.WithOpenApi();

// ============================================
// WSL Service Management
// ============================================
var wslServices = api.MapGroup("/wsl/services");

wslServices.MapGet("", async (IWslServiceOrchestrator orchestrator) =>
{
    var services = await orchestrator.GetAllServicesStatusAsync();
    return Results.Ok(services);
})
.WithName("GetAllServices")
.WithOpenApi();

wslServices.MapPost("/{name}/start", async (
    string name,
    IWslServiceOrchestrator orchestrator) =>
{
    var result = await orchestrator.StartServiceAsync(name);
    return result
        ? Results.Ok(new { success = true, message = $"Service {name} started" })
        : Results.Problem(
            detail: $"Failed to start service {name}",
            statusCode: 500,
            title: "Service Start Failed");
})
.WithName("StartService")
.WithOpenApi();

// ============================================
// RAG Endpoints (existing, kept for compatibility)
// ============================================
var rag = app.MapGroup("/rag");

rag.MapPost("/upload", async (HttpRequest req, IHttpClientFactory f) =>
{
    // [Keep existing RAG upload logic as-is]
    var mode = req.Query["mode"].ToString();
    var useSentences = string.Equals(mode, "sentences", StringComparison.OrdinalIgnoreCase);
    // ... rest of existing code
})
.WithName("RagUpload")
.WithOpenApi();

rag.MapPost("/query", async (HttpRequest req, IHttpClientFactory f) =>
{
    // [Keep existing RAG query logic as-is]
    var body = await JsonSerializer.DeserializeAsync<QueryBody>(req.Body,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new QueryBody("", 3);
    // ... rest of existing code
})
.WithName("RagQuery")
.WithOpenApi();

var audit = api.MapGroup("/audit");

audit.MapGet("/logs", async (
    [FromServices] IAuditLogger auditLogger, 
    [FromQuery] DateTime? startDate,
    [FromQuery] DateTime? endDate,
    [FromQuery] string? entityId,
    [FromQuery] int limit = 100) =>          
{
    var filter = new AuditLogFilter
    {
        StartDate = startDate,
        EndDate = endDate,
        EntityId = entityId,
        Limit = limit
    };

    var logs = await auditLogger.GetAuditLogsAsync(filter);
    return Results.Ok(logs);
})
.WithName("GetAuditLogs")
.RequireAuthorization("AdminOnly");

// Start the application
app.Run("http://localhost:5080");

// ============================================
// Request/Response DTOs (move to separate file later)
// ============================================
public record GenerateRequest(
    string ModelId,
    string Prompt,
    Dictionary<string, object>? Parameters = null,
    HashSet<string>? Tags = null
);

public record QueryBody(string query, int k);

public record FileSyncRequest(string WindowsPath, string WslPath);

public record ProcessingRequest(
    string ProcessingType,
    Dictionary<string, object>? Parameters = null
);

public record ExportRequest(
    string? ExportPath = null,
    bool IncludeProcessedVersions = true,
    bool GenerateVerificationScript = true
);

// [Keep other helper classes at the bottom]