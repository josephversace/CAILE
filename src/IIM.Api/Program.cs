using System.Text.Json;
using System.Text.Json.Serialization;
using GraphRag;
using GraphRag.Storage.Neo4j;
using Hangfire;
using IIM.Api.Endpoints;
using IIM.Api.Extensions;
using IIM.Api.Filters;
using IIM.Api.Hubs;
using IIM.Api.Services;
using IIM.Infrastructure.Data;
using IIM.Infrastructure.Embeddings;
using IIM.Infrastructure.Foundry;
using IIM.Infrastructure.Services;
using IIM.Ingestion.Interfaces;
using IIM.Ingestion.Services;
using IIM.Shared.Configuration;
using IIM.Shared.Interfaces;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.AI;   


var builder = WebApplication.CreateBuilder(args);

// ============================================
// Load deployment configuration
// ============================================
var deploymentConfig = new DeploymentConfiguration();
builder.Configuration.GetSection("Deployment").Bind(deploymentConfig);

// bind Deployment first
var deployment = builder.Configuration.GetSection("Deployment").Get<DeploymentConfiguration>();

builder.Services
	.AddBoundConfiguration(builder.Configuration)
	.AddIdentityAndAuth(builder.Configuration, deployment)
	.AddIIMDatabases(builder.Configuration)
	.AddInfrastructureLayer(builder.Configuration)
	.AddCoreLayer(builder.Configuration, deployment)
	.AddAgentsLayer()
	.AddApplicationLayer()
	.AddApiLayer(builder.Configuration, deployment)
	.AddIngestionLayer()
	.AddHostedWorkers(builder.Configuration);






// ============================================
// Swagger / OpenAPI
// ============================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
	options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
	{
		Title = "IIM API",
		Version = "v1",
		Description = "Intelligent Data Governance Machine API",
		Contact = new Microsoft.OpenApi.OpenApiContact
		{
			Name = "IIM Team",
			Email = "support@iim.local"
		}
	});
});

// ============================================
// JSON Options
// ============================================
builder.Services.ConfigureHttpJsonOptions(opts =>
{
	opts.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
	opts.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
	opts.SerializerOptions.WriteIndented = false;
});

// ============================================
// Health Checks
// ============================================
builder.Services.AddHealthChecks();

// ============================================
// Response Compression for SignalR
// ============================================
builder.Services.AddResponseCompression(opts =>
{
	opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
		new[] { "application/octet-stream" });
});

// ============================================
// CORS for Blazor Client
// ============================================
var corsPolicy = "_caileCors";
builder.Services.AddCors(options =>
{
	options.AddPolicy(corsPolicy, policy =>
	{
		policy
			.WithOrigins(
	"http://localhost:5056",
	"https://localhost:5056",
	"https://localhost:5080"
)

			.AllowAnyHeader()
			.AllowAnyMethod()
			.AllowCredentials();
	});
});




builder.Services.AddAGUI();



builder.WebHost.ConfigureKestrel(k =>
{
	k.ListenLocalhost(5080, opt => {

		opt.UseHttps();
	});
});

var app = builder.Build();

// ============================================
// Preload Models Aync
// ============================================

using (var scope = app.Services.CreateScope())
{
	var migrator = scope.ServiceProvider
		.GetRequiredService<DatabaseMigrationRunner>();
	await migrator.ApplyAllMigrationsAsync();

	var vision = scope.ServiceProvider
		.GetRequiredService<IMultimodalVisionService>();
	_ = Task.Run(() => vision.InitializeAsync());

	var embedding = scope.ServiceProvider
		.GetRequiredService<IEmbeddingService>();
	_ = Task.Run(() => embedding.InitializeAsync());

	var qdrant = scope.ServiceProvider.GetRequiredService<IQdrantService>();
	await qdrant.EnsureCollectionAsync();
}



// ============================================
// Swagger Middleware
// ============================================
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI(options =>
	{
		options.SwaggerEndpoint("/swagger/v1/swagger.json", "IIM API v1");
		options.RoutePrefix = "swagger";
	});
}

// ============================================
// Core Pipeline
// ============================================
app.UseResponseCompression();

app.UseCors(corsPolicy);

app.UseAuthentication();
app.UseAuthorization();

//if (deploymentConfig.RequireAuth)
//{
//	app.UseAuthentication();
//	app.UseAuthorization();
//}

// ============================================
// Health Check Endpoints
// ============================================
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
	Predicate = check => check.Tags.Contains("ready")
});
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
	Predicate = _ => true
});

// ============================================
// SignalR Hubs
// ============================================
app.MapHub<WorkspaceHub>("/hubs/workspace");

if (deploymentConfig.Mode == DeploymentMode.ClientUI)
{
	app.MapHub<AdminHub>("/hubs/admin");
	app.MapRazorPages();
}

// ============================================
// Hangfire Dashboard
// ============================================
if (app.Environment.IsDevelopment() ||
	deploymentConfig.Mode == DeploymentMode.ServerNode)
{
	app.UseHangfireDashboard("/hangfire", new DashboardOptions
	{
		Authorization = new[] { new HangfireAuthorizationFilter() },
		IgnoreAntiforgeryToken = true
	});
}

app.UseWhen(ctx => ctx.Request.Path.StartsWithSegments("/ai"), branch =>
{
	branch.Use(async (ctx, next) =>
	{
		// Force-disable compression for AI streaming
		ctx.Response.Headers["Content-Encoding"] = "identity";
		await next();
	});
});


// ============================================
// Map endpoint groups
// ============================================

app.MapModelRegistryEndpoints();
app.MapFileEndpoints();
app.MapWorkspaceEndpoints();
app.MapRagEndpoints();
app.MapAuthEndpoints();
app.MapIngestionEndpoints();
app.MapSetupEndpoints();
app.MapAIEndpoints();
app.MapAttachmentEndpoints();

// AG-UI endpoint for reasoning (uses MapAGUI directly)
//using (var scope = app.Services.CreateScope())
//{

//	var agentFactory = scope.ServiceProvider.GetRequiredService<IAIAgentFactory>();
//	var agent = await agentFactory.GetChatAgentAsync();
//	app.MapAGUI("/ai/reason-ui", agent);
//}


// ============================================
// Start the application
// ============================================


app.Run();
