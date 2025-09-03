// File: Program.cs
// License: MIT or Apache-2.0
//
// Minimal API composition root. Wires DI, middleware, and HTTP endpoints.
// All corrections vs. your original are annotated with:  // FIX:

using ForensicS3Storage.Implementations;
using ForensicS3Storage.Interfaces;
using ForensicS3Storage.Models;
using IIM.S3.Interfaces;
using IIM.S3.Models;
using IIM.S3.Services;
using Microsoft.AspNetCore.WebUtilities;

var builder = WebApplication.CreateBuilder(args);

// ---- Paths / settings ----
var cfg = builder.Configuration;
var basePath = cfg["Storage:BasePath"] ?? "/var/lib/forensic-s3";
var casPath = cfg["Storage:CasPath"] ?? Path.Combine(basePath, ".cas");
var tempPath = cfg["Storage:TempPath"] ?? Path.Combine(basePath, ".temp");
var dbPath = Path.Combine(basePath, "metadata.sqlite");

Directory.CreateDirectory(basePath);
Directory.CreateDirectory(casPath);
Directory.CreateDirectory(tempPath);

// ---- DI container ----
builder.Services.AddSingleton<IClock, UtcClock>();
builder.Services.AddSingleton(new StoragePaths(basePath, casPath, tempPath, dbPath));

builder.Services.AddSingleton<IStorageBackend, FileSystemBackend>();
builder.Services.AddSingleton<IMetadataStore, SqliteMetadataStore>();
builder.Services.AddSingleton<IEventBus, LoggingEventBus>(); // Replace with Kafka when ready
builder.Services.AddSingleton<IDeduplicationService, ContentAddressableStorage>();

// FIX: honest, verifiable presign scheme (CamSig) instead of faux SigV4
builder.Services.AddSingleton<IPresignedUrlService, CamSigPresignedUrlService>();

// FIX: SMB hardened (no guest, no wide links, path jail)
builder.Services.AddSingleton<ISmbShareService, SmbShareService>();

// FIX: single-pass hashing that writes while hashing non-seekable streams
builder.Services.AddSingleton<IHashingService, HashingService>();

builder.Services.AddSingleton<IPolicyEngine, PolicyEngine>();
builder.Services.AddSingleton<IS3Service, S3Service>();

builder.Services.AddHostedService<SmbShareSyncService>();
builder.Services.AddHostedService<ObjectLockEnforcer>();

builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.TypeInfoResolverChain.Insert(0, ApiJsonContext.Default));


builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// ---- one-time init ----
app.Services.GetRequiredService<IMetadataStore>().Init();
app.Services.GetRequiredService<ISmbShareService>().InitializeShares();

// ---- Middleware: presigned URL gate ----
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Query.ContainsKey("X-CamSig"))
    {
        var presigned = ctx.RequestServices.GetRequiredService<IPresignedUrlService>();
        var url = $"{ctx.Request.Scheme}://{ctx.Request.Host}{ctx.Request.Path}{ctx.Request.QueryString}";
        if (!await presigned.ValidateAsync(url, ctx.Request.Method))
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            await ctx.Response.WriteAsync("Invalid or expired presigned URL");
            return;
        }
    }
    await next();
});

// ---------------------- Endpoints ----------------------

// Admin: create bucket
app.MapPost("/admin/buckets", async (BucketCreateRequest req, IS3Service s3) =>
{
    await s3.CreateBucketAsync(req.Bucket, req.Config ?? new BucketConfiguration());
    return Results.Ok();
});

// Admin: presign
app.MapPost("/admin/presign", async (PresignRequest req, IS3Service s3) =>
{
    var url = await s3.GeneratePresignedUrlAsync(req.Bucket, req.Key, req.Operation, req.ExpirySeconds);
    return Results.Json(new { url });
});

// List
app.MapGet("/{bucket}", async (string bucket, string? prefix, string? delimiter, IS3Service s3) =>
{
    var r = await s3.ListObjectsAsync(bucket, prefix ?? "", delimiter);
    return Results.Json(r);
});

// HEAD
app.MapMethods("/{bucket}/{**key}", new[] { "HEAD" }, async (string bucket, string key, HttpResponse res, IS3Service s3) =>
{
    await s3.HeadObjectAsync(bucket, key, res);
    return Results.Empty;
});

// GET (supports Range)
app.MapGet("/{bucket}/{**key}", async (string bucket, string key, HttpRequest req, HttpResponse res, IS3Service s3) =>
{
    await s3.GetObjectAsync(bucket, key, req, res);
    return Results.Empty;
});

// PUT (single or part upload)
app.MapPut("/{bucket}/{**key}", async (string bucket, string key, HttpRequest req, HttpResponse res, IS3Service s3) =>
{
    // Multipart part upload ?uploadId=&partNumber=
    if (req.Query.TryGetValue("uploadId", out var upId) &&
        req.Query.TryGetValue("partNumber", out var pnStr) &&
        int.TryParse(pnStr, out var pn))
    {
        var etag = await s3.UploadPartAsync(upId!, pn, req);
        res.Headers.ETag = etag; // S3 returns ETag in header
        res.StatusCode = StatusCodes.Status200OK;
        return Results.Empty;
    }

    // Single-part
    var meta = await s3.PutObjectAsync(bucket, key, req);
    res.Headers.ETag = $"\"{meta.MD5}\"";
    res.Headers["x-amz-version-id"] = meta.VersionId;
    res.StatusCode = StatusCodes.Status200OK;
    return Results.Empty;
});

// POST (?uploads to initiate, ?uploadId= to complete)
app.MapPost("/{bucket}/{**key}", async (string bucket, string key, HttpRequest req, HttpResponse res, IS3Service s3) =>
{
    if (req.Query.ContainsKey("uploads"))
    {
        var uploadId = await s3.InitiateMultipartUploadAsync(bucket, key);
        return Results.Json(new { UploadId = uploadId });
    }

    if (req.Query.TryGetValue("uploadId", out var upId))
    {
        var parts = await req.ReadFromJsonAsync<List<CompletePart>>() ?? new();
        var ordered = parts.OrderBy(p => p.PartNumber).Select(p => (p.PartNumber, p.ETag)).ToList();
        var (meta, multipartEtag) = await s3.CompleteMultipartUploadAsync(upId!, ordered);
        // FIX: multipart ETag parity with S3
        res.Headers.ETag = $"\"{multipartEtag}\"";
        res.Headers["x-amz-version-id"] = meta.VersionId;
        res.StatusCode = StatusCodes.Status200OK;
        return Results.Empty;
    }

    return Results.BadRequest("Missing ?uploads or ?uploadId");
});

// DELETE (soft delete, WORM enforced)
app.MapDelete("/{bucket}/{**key}", async (string bucket, string key, HttpRequest req, IS3Service s3) =>
{
    var ok = await s3.DeleteObjectAsync(bucket, key, req);
    return ok ? Results.NoContent() : Results.NotFound();
});

app.Run();
