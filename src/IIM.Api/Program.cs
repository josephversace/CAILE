using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Net.Http.Json;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using UglyToad.PdfPig;
using IIM.Core.Inference;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using IIM.Core.Security;
using IIM.Shared.Models;
using IIM.Core.Services;
using IIM.Core.AI;
using InsufficientMemoryException = IIM.Core.Models.InsufficientMemoryException;
using IIM.Shared.Enums;
using IIM.Infrastructure.Platform;
using IIM.Core.Models;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add HTTP clients
builder.Services.AddHttpClient("qdrant", c =>
    c.BaseAddress = new Uri(builder.Configuration["Qdrant:BaseUrl"] ?? "http://localhost:6333"));
builder.Services.AddHttpClient("embed", c =>
    c.BaseAddress = new Uri(builder.Configuration["EmbedService:BaseUrl"] ?? "http://localhost:8081"));
builder.Services.AddHttpClient("wsl", c =>
    c.Timeout = TimeSpan.FromSeconds(30));
builder.Services.AddHttpClient("wsl-services", c =>
    c.Timeout = TimeSpan.FromSeconds(10));

// Add Core services
builder.Services.AddSingleton<IModelOrchestrator, DefaultModelOrchestrator>();
builder.Services.AddSingleton<IInferencePipeline, InferencePipeline>();
builder.Services.AddSingleton<IWslManager, WslManager>();
builder.Services.AddSingleton<IWslServiceOrchestrator, WslServiceOrchestrator>();

// Add the WSL service orchestrator as a hosted service
builder.Services.AddHostedService<WslServiceOrchestrator>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck<WslHealthCheck>("wsl", tags: new[] { "wsl", "infrastructure" });

// Add CORS for Blazor app
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazor", policy =>
    {
        policy.WithOrigins("http://localhost:5000", "http://localhost:5001")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Add memory cache for performance
builder.Services.AddMemoryCache();

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add Evidence Management
builder.Services.AddSingleton<IEvidenceManager>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<EvidenceManager>>();
    var config = new EvidenceConfiguration
    {
        StorePath = builder.Configuration["Evidence:StorePath"] ??
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IIM", "Evidence"),
        EnableEncryption = builder.Configuration.GetValue<bool>("Evidence:EnableEncryption", false),
        RequireDualControl = builder.Configuration.GetValue<bool>("Evidence:RequireDualControl", false),
        MaxFileSizeMb = builder.Configuration.GetValue<int>("Evidence:MaxFileSizeMb", 10240)
    };
    return new EvidenceManager(logger, config);
});

// Register the background service
builder.Services.AddHostedService<EvidenceIntegrityMonitor>();

builder.Services.AddSingleton<IModelOrchestrator, S>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowBlazor");

// Health check endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});

// ============================================
// System Status Endpoints
// ============================================

app.MapGet("/healthz", () => Results.Text("ok"))
    .WithName("HealthCheck")
    .WithOpenApi();

app.MapGet("/v1/gpu", async (IModelOrchestrator orchestrator) =>
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

app.MapGet("/v1/stats", async (IInferencePipeline pipeline, IModelOrchestrator orchestrator) =>
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
// Model Management Endpoints
// ============================================
app.MapPost("/v1/models/load", async (
    [FromBody] ApiModelRequest request,
    IModelOrchestrator orchestrator,
    ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("Loading model {ModelId}", request.ModelId);
        
        // Convert ApiModelRequest to IIM.Core.Inference.ModelRequest
        var modelRequest = new ModelRequest
        {
            ModelId = request.ModelId,
            ModelPath = request.ModelPath,
            ModelType = ModelType.LLM


        };
        
        var handle = await orchestrator.LoadModelAsync(modelRequest);
        
        return Results.Ok(new
        {
            success = true,
            modelId = handle.ModelId,
            sessionId = handle.SessionId,
            message = $"Model {request.ModelId} loaded successfully"
        });
    }
    catch (InsufficientMemoryException ex)
    {
        logger.LogWarning(ex, "Insufficient memory to load model");
        return Results.Problem(
            detail: ex.Message,
            statusCode: 507, // Insufficient Storage
            title: "Insufficient Memory");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to load model");
        return Results.Problem(
            detail: ex.Message,
            statusCode: 500,
            title: "Model Load Failed");
    }
})
.WithName("LoadModel")
.WithOpenApi();

app.MapPost("/v1/models/{modelId}/unload", async (
    string modelId,
    IModelOrchestrator orchestrator) =>
{
    await orchestrator.UnloadModelAsync(modelId);
    return Results.Ok(new { success = true, message = $"Model {modelId} unloaded" });
})
.WithName("UnloadModel")
.WithOpenApi();

// ============================================
// Inference Endpoints
// ============================================

app.MapPost("/v1/generate", async (
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

app.MapPost("/v1/generate/batch", async (
    [FromBody] List<GenerateRequest> requests,
    IInferencePipeline pipeline) =>
{
    var pipelineRequests = requests.Select((r, i) => new InferencePipelineRequest
    {
        ModelId = r.ModelId,
        Input = r.Prompt,
        Parameters = r.Parameters,
        Tags = r.Tags,
        Index = i
    });

    var results = await pipeline.ExecuteBatchAsync<string>(pipelineRequests);
    return Results.Json(results);
})
.WithName("GenerateBatch")
.WithOpenApi();

// ============================================
// WSL Management Endpoints
// ============================================

app.MapGet("/api/wsl/status", async (IWslManager wsl) =>
{
    var status = await wsl.GetStatusAsync();
    return Results.Ok(status);
})
.WithName("GetWslStatus")
.WithOpenApi();

app.MapGet("/api/wsl/network", async (IWslManager wsl) =>
{
    var network = await wsl.GetNetworkInfoAsync("IIM-Ubuntu");
    return Results.Ok(network);
})
.WithName("GetWslNetwork")
.WithOpenApi();

app.MapGet("/api/wsl/health", async (IWslManager wsl) =>
{
    var health = await wsl.HealthCheckAsync();
    return health.IsHealthy ? Results.Ok(health) : Results.Problem(
        detail: string.Join("; ", health.Issues),
        statusCode: 503,
        title: "WSL Unhealthy");
})
.WithName("GetWslHealth")
.WithOpenApi();

app.MapPost("/api/wsl/ensure", async (IWslManager wsl) =>
{
    var distro = await wsl.EnsureDistroAsync();
    return Results.Ok(distro);
})
.WithName("EnsureWslDistro")
.WithOpenApi();

// ============================================
// WSL Service Management Endpoints
// ============================================

app.MapGet("/api/wsl/services", async (IWslServiceOrchestrator orchestrator) =>
{
    var services = await orchestrator.GetAllServicesStatusAsync();
    return Results.Ok(services);
})
.WithName("GetAllServices")
.WithOpenApi();

app.MapGet("/api/wsl/services/{name}", async (
    string name,
    IWslServiceOrchestrator orchestrator) =>
{
    var status = await orchestrator.GetServiceStatusAsync(name);
    return status.State == ServiceState.NotFound
        ? Results.NotFound(status)
        : Results.Ok(status);
})
.WithName("GetServiceStatus")
.WithOpenApi();

app.MapPost("/api/wsl/services/{name}/start", async (
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

app.MapPost("/api/wsl/services/{name}/stop", async (
    string name,
    IWslServiceOrchestrator orchestrator) =>
{
    var result = await orchestrator.StopServiceAsync(name);
    return result
        ? Results.Ok(new { success = true, message = $"Service {name} stopped" })
        : Results.Problem(
            detail: $"Failed to stop service {name}",
            statusCode: 500,
            title: "Service Stop Failed");
})
.WithName("StopService")
.WithOpenApi();

app.MapPost("/api/wsl/services/{name}/restart", async (
    string name,
    IWslServiceOrchestrator orchestrator) =>
{
    var result = await orchestrator.RestartServiceAsync(name);
    return result
        ? Results.Ok(new { success = true, message = $"Service {name} restarted" })
        : Results.Problem(
            detail: $"Failed to restart service {name}",
            statusCode: 500,
            title: "Service Restart Failed");
})
.WithName("RestartService")
.WithOpenApi();

app.MapPost("/api/wsl/services/ensure-all", async (IWslServiceOrchestrator orchestrator) =>
{
    var result = await orchestrator.EnsureAllServicesAsync();
    return result
        ? Results.Ok(new { success = true, message = "All critical services started" })
        : Results.Problem(
            detail: "Some critical services failed to start",
            statusCode: 500,
            title: "Service Startup Failed");
})
.WithName("EnsureAllServices")
.WithOpenApi();

// ============================================
// RAG Endpoints (existing, updated)
// ============================================

var collection = "rag_index";

app.MapPost("/rag/upload", async (HttpRequest req, IHttpClientFactory f) =>
{
    var mode = req.Query["mode"].ToString();
    var useSentences = string.Equals(mode, "sentences", StringComparison.OrdinalIgnoreCase);

    if (!req.HasFormContentType) return Results.BadRequest("multipart/form-data required");
    var form = await req.ReadFormAsync();
    int count = 0;

    foreach (var file in form.Files)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var validExtensions = new[] { ".txt", ".md", ".pdf", ".docx" };
        if (!validExtensions.Contains(ext)) continue;

        string text;
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        ms.Position = 0;

        if (ext == ".pdf") text = ExtractTextFromPdf(ms);
        else if (ext == ".docx") text = ExtractTextFromDocx(ms);
        else
        {
            using var sr = new StreamReader(ms, Encoding.UTF8, true);
            text = await sr.ReadToEndAsync();
        }

        var chunks = useSentences ? ChunkBySentences(text) : ChunkFixed(text);
        int idx = 0;

        foreach (var ch in chunks)
        {
            var vec = await EmbedAsync(f, ch.text);
            await UpsertAsync(f, collection, $"{file.FileName}:{idx++}", vec,
                new Dictionary<string, object> { { "file", file.FileName }, { "chunk", ch.text } });
        }
        count++;
    }

    return Results.Ok(new { status = "indexed", files = count, mode = useSentences ? "sentences" : "fixed" });
})
.WithName("RagUpload")
.WithOpenApi();

app.MapPost("/agents/rag/query", async (HttpRequest req, IHttpClientFactory f) =>
{
    var body = await JsonSerializer.DeserializeAsync<QueryBody>(req.Body,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new QueryBody("", 3);

    var qv = await EmbedAsync(f, body.query ?? "");
    var hits = await SearchAsync(f, collection, qv, body.k > 0 ? body.k : 3);
    var context = string.Join("\n---\n", hits.Select(h =>
        h.payload.TryGetValue("chunk", out var c) ? c?.ToString() : ""));

    var answer = $"Based on the indexed documents:\n{context}";
    var citations = hits.Select(h => h.id).ToList();

    return Results.Json(new { answer, citations });
})
.WithName("RagQuery")
.WithOpenApi();

// ============================================
// File Sync Endpoints
// ============================================

app.MapPost("/api/wsl/sync", async (
    [FromBody] FileSyncRequest request,
    IWslManager wsl) =>
{
    var result = await wsl.SyncFilesAsync(request.WindowsPath, request.WslPath);
    return result
        ? Results.Ok(new { success = true, message = "Files synced successfully" })
        : Results.Problem(
            detail: "Failed to sync files",
            statusCode: 500,
            title: "Sync Failed");
})
.WithName("SyncFiles")
.WithOpenApi();

// ============================================
// Evidence Management Endpoints
// ============================================

app.MapPost("/api/evidence/ingest", async (
    HttpRequest request,
    IEvidenceManager evidenceManager,
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

        // Validate required fields
        if (string.IsNullOrEmpty(metadata.CaseNumber) || string.IsNullOrEmpty(metadata.CollectedBy))
        {
            return Results.BadRequest("Case number and collected by are required");
        }

        // Ingest the evidence
        using var stream = file.OpenReadStream();
        var evidence = await evidenceManager.IngestEvidenceAsync(
            stream,
            file.FileName,
            metadata);

        logger.LogInformation("Evidence ingested: {EvidenceId} for case {CaseNumber}",
            evidence.Id, evidence.CaseNumber);

        return Results.Ok(new
        {
            evidenceId = evidence.Id,
            fileName = evidence.OriginalFileName,
            fileSize = evidence.FileSize,
            hashes = evidence.Hashes,
            signature = evidence.Signature,
            message = "Evidence ingested successfully"
        });
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
.WithOpenApi()
.DisableAntiforgery(); // Required for file uploads

app.MapPost("/api/evidence/{evidenceId}/verify", async (
    string evidenceId,
    IEvidenceManager evidenceManager) =>
{
    try
    {
        var isValid = await evidenceManager.VerifyIntegrityAsync(evidenceId);

        return Results.Ok(new
        {
            evidenceId,
            integrityValid = isValid,
            timestamp = DateTimeOffset.UtcNow,
            message = isValid ? "Integrity verified" : "Integrity check failed"
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.Message,
            statusCode: 500,
            title: "Verification Failed");
    }
})
.WithName("VerifyEvidence")
.WithOpenApi();

app.MapGet("/api/evidence/{evidenceId}/chain", async (
    string evidenceId,
    IEvidenceManager evidenceManager) =>
{
    try
    {
        var report = await evidenceManager.GenerateChainOfCustodyAsync(evidenceId);
        return Results.Ok(report);
    }
    catch (EvidenceNotFoundException)
    {
        return Results.NotFound($"Evidence {evidenceId} not found");
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.Message,
            statusCode: 500,
            title: "Failed to Generate Report");
    }
})
.WithName("GetChainOfCustody")
.WithOpenApi();

app.MapPost("/api/evidence/{evidenceId}/process", async (
    string evidenceId,
    [FromBody] ProcessingRequest request,
    IEvidenceManager evidenceManager,
    IInferencePipeline pipeline,
    ILogger<Program> logger) =>
{
    try
    {
        // Process evidence through AI pipeline
        var processed = await evidenceManager.ProcessEvidenceAsync(
            evidenceId,
            request.ProcessingType,
            async (inputStream) =>
            {
                // This is where AI processing happens
                // For example: OCR, transcription, image analysis, etc.

                if (request.ProcessingType == "TRANSCRIBE")
                {
                    // Use Whisper model to transcribe audio
                    var pipelineRequest = new InferencePipelineRequest
                    {
                        ModelId = "whisper-large",
                        Input = inputStream,
                        Tags = new HashSet<string> { "evidence", "transcription" }
                    };

                    var result = await pipeline.ExecuteAsync<Stream>(pipelineRequest);
                    return result;
                }
                else if (request.ProcessingType == "OCR")
                {
                    // Use OCR model to extract text
                    // Implementation here
                }
                else if (request.ProcessingType == "IMAGE_ANALYSIS")
                {
                    // Use CLIP or other vision model
                    // Implementation here
                }

                // Default: return input unchanged
                return inputStream;
            });

        logger.LogInformation("Evidence {EvidenceId} processed with {Type}",
            evidenceId, request.ProcessingType);

        return Results.Ok(new
        {
            processedId = processed.Id,
            originalEvidenceId = processed.OriginalEvidenceId,
            processingType = processed.ProcessingType,
            processedHash = processed.ProcessedHash,
            message = "Evidence processed successfully"
        });
    }
    catch (EvidenceNotFoundException)
    {
        return Results.NotFound($"Evidence {evidenceId} not found");
    }
    catch (IntegrityException ex)
    {
        return Results.Problem(
            detail: ex.Message,
            statusCode: 409,
            title: "Integrity Check Failed");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to process evidence");
        return Results.Problem(
            detail: ex.Message,
            statusCode: 500,
            title: "Processing Failed");
    }
})
.WithName("ProcessEvidence")
.WithOpenApi();

app.MapPost("/api/evidence/{evidenceId}/export", async (
    string evidenceId,
    [FromBody] ExportRequest request,
    IEvidenceManager evidenceManager) =>
{
    try
    {
        var exportPath = request.ExportPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "IIM_Exports");

        var export = await evidenceManager.ExportEvidenceAsync(evidenceId, exportPath);

        return Results.Ok(new
        {
            exportId = export.ExportId,
            evidenceId = export.EvidenceId,
            exportPath = export.ExportPath,
            files = export.Files,
            integrityValid = export.IntegrityValid,
            message = $"Evidence exported to {export.ExportPath}"
        });
    }
    catch (EvidenceNotFoundException)
    {
        return Results.NotFound($"Evidence {evidenceId} not found");
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.Message,
            statusCode: 500,
            title: "Export Failed");
    }
})
.WithName("ExportEvidence")
.WithOpenApi();

app.MapGet("/api/evidence/{evidenceId}/audit", async (
    string evidenceId,
    IEvidenceManager evidenceManager) =>
{
    try
    {
        var auditLog = await evidenceManager.GetAuditLogAsync(evidenceId);
        return Results.Ok(auditLog);
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.Message,
            statusCode: 500,
            title: "Failed to Retrieve Audit Log");
    }
})
.WithName("GetAuditLog")
.WithOpenApi();
app.MapGet("/api/evidence/list", async (
    IEvidenceManager evidenceManager,  // Required parameter FIRST
    [FromQuery] string? caseNumber,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20) =>
{
    try
    {
        // In production, this would query from a database
        // For now, return sample data
        var items = new List<object>
        {
            new
            {
                id = Guid.NewGuid().ToString("N"),
                originalFileName = "suspect_device.dd",
                caseNumber = caseNumber ?? "2024-CF-1234",
                ingestTimestamp = DateTimeOffset.UtcNow.AddDays(-5),
                fileSize = 1024L * 1024 * 1024 * 32, // 32GB
                integrityValid = true,
                chainLength = 5
            }
        };

        return Results.Ok(new
        {
            items,
            page,
            pageSize,
            totalCount = items.Count
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.Message,
            statusCode: 500,
            title: "Failed to List Evidence");
    }
})
.WithName("ListEvidence")
.WithOpenApi();

// Start the application
app.Run("http://localhost:5080");

// ============================================
// Helper Functions
// ============================================

static IEnumerable<(int idx, string text)> ChunkFixed(string text, int maxLen = 800, int overlap = 100)
{
    var clean = Regex.Replace(text ?? "", @"\s+", " ").Trim();
    int i = 0;
    int idx = 0;

    while (i < clean.Length)
    {
        int end = Math.Min(i + maxLen, clean.Length);
        string slice = clean.Substring(i, end - i);
        yield return (idx++, slice);

        if (end == clean.Length) break;

        i = end - overlap;
        if (i < 0) i = 0;
    }
}

static IEnumerable<(int idx, string text)> ChunkBySentences(string text, int maxChars = 800)
{
    var sents = Regex.Split(text ?? "", @"(?<=[\.!?])\s+");
    var sb = new StringBuilder();
    int idx = 0;

    foreach (var s in sents)
    {
        if (sb.Length + s.Length + 1 > maxChars && sb.Length > 0)
        {
            yield return (idx++, sb.ToString().Trim());
            sb.Clear();
        }
        sb.Append(s).Append(' ');
    }

    if (sb.Length > 0) yield return (idx++, sb.ToString().Trim());
}

static string ExtractTextFromPdf(Stream stream)
{
    var sb = new StringBuilder();
    using (var doc = PdfDocument.Open(stream))
    {
        foreach (var page in doc.GetPages())
            sb.AppendLine(page.Text);
    }
    return sb.ToString();
}

static string ExtractTextFromDocx(Stream stream)
{
    var sb = new StringBuilder();
    using (var doc = WordprocessingDocument.Open(stream, false))
    {
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body != null)
        {
            foreach (var para in body.Descendants<Paragraph>())
                sb.AppendLine(string.Concat(para.Descendants<Text>().Select(t => t.Text)));
        }
    }
    return sb.ToString();
}

static async Task<float[]> EmbedAsync(IHttpClientFactory f, string text)
{
    var http = f.CreateClient("embed");
    var res = await http.PostAsJsonAsync("/embed", new { text });
    res.EnsureSuccessStatusCode();
    var result = await res.Content.ReadFromJsonAsync<EmbedResponse>();
    return result?.embedding ?? Array.Empty<float>();
}

static async Task UpsertAsync(IHttpClientFactory f, string collection, string id, float[] vec, Dictionary<string, object> payload)
{
    var http = f.CreateClient("qdrant");

    // Create collection if it doesn't exist
    var create = new { vectors = new { size = vec.Length, distance = "Cosine" } };
    await http.PutAsJsonAsync($"/collections/{collection}", create);

    // Upsert point
    var body = new { points = new[] { new { id, vector = vec, payload } } };
    await http.PutAsJsonAsync($"/collections/{collection}/points", body);
}

static async Task<List<(string id, Dictionary<string, object> payload)>> SearchAsync(
    IHttpClientFactory f, string collection, float[] qv, int k)
{
    var http = f.CreateClient("qdrant");
    var body = new { vector = qv, limit = k, with_payload = true };
    var res = await http.PostAsJsonAsync($"/collections/{collection}/points/search", body);
    var json = await res.Content.ReadFromJsonAsync<QdrantSearchResponse>() ?? new();
    return json.result.Select(r => (r.id, r.payload ?? new())).ToList();
}

// ============================================
// Request/Response Models
// ============================================

public record GenerateRequest(
    string ModelId,
    string Prompt,
    Dictionary<string, object>? Parameters = null,
    HashSet<string>? Tags = null
);

public record ApiModelRequest(
    string ModelId,
    string? ModelPath = null,
    Dictionary<string, object>? Options = null
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

public record EmbedResponse(float[] embedding);

// Fix: Move Item class outside and rename it
public class QdrantSearchItem
{
    public string id { get; set; } = string.Empty;
    public Dictionary<string, object>? payload { get; set; }
}

public record QdrantSearchResponse(List<QdrantSearchItem>? result = null)
{
    public List<QdrantSearchItem> Result { get; } = result ?? new();
}

// ============================================
// Background Service for Integrity Monitoring
// ============================================

public class EvidenceIntegrityMonitor : BackgroundService
{
    private readonly IEvidenceManager _evidenceManager;
    private readonly ILogger<EvidenceIntegrityMonitor> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(6);

    public EvidenceIntegrityMonitor(
        IEvidenceManager evidenceManager,
        ILogger<EvidenceIntegrityMonitor> logger)
    {
        _evidenceManager = evidenceManager;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Starting evidence integrity check");

                // In production, get list of evidence IDs from database
                // For now, this is a placeholder
                var evidenceIds = new List<string>();

                foreach (var evidenceId in evidenceIds)
                {
                    try
                    {
                        var isValid = await _evidenceManager.VerifyIntegrityAsync(evidenceId, stoppingToken);

                        if (!isValid)
                        {
                            _logger.LogError("Integrity check failed for evidence {EvidenceId}", evidenceId);
                            // Send alert to administrators
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error checking evidence {EvidenceId}", evidenceId);
                    }
                }

                _logger.LogInformation("Evidence integrity check completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in integrity monitoring");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }
}

// ============================================
// Health Check Implementation
// ============================================

public class WslHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
{
    private readonly IWslManager _wslManager;

    public WslHealthCheck(IWslManager wslManager)
    {
        _wslManager = wslManager;
    }

    public async Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var health = await _wslManager.HealthCheckAsync(cancellationToken);

        if (health.IsHealthy)
        {
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(
                "WSL is healthy",
                new Dictionary<string, object>
                {
                    ["wsl_ready"] = health.WslReady,
                    ["distro_running"] = health.DistroRunning,
                    ["services_healthy"] = health.ServicesHealthy,
                    ["network_connected"] = health.NetworkConnected
                });
        }

        return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(
            "WSL health check failed",
            data: new Dictionary<string, object>
            {
                ["issues"] = health.Issues
            });
    }
}