using System.Text;
using IIM.Shared.Dtos;
using IIM.Shared.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntimeGenAI;


namespace IIM.Infrastructure.Services;
public sealed class MultimodalVisionService : IMultimodalVisionService, IDisposable, IAsyncDisposable
{
	private readonly ILogger<MultimodalVisionService> _logger;
	private readonly IServiceScopeFactory _scopeFactory;

	private Model? _model;
	private MultiModalProcessor? _processor;
	private Tokenizer? _tokenizer;
	private readonly SemaphoreSlim _semaphore = new(1, 1);

	private bool _disposed;

	public bool IsReady => _model is not null && !_disposed;

	public MultimodalVisionService(
		IServiceScopeFactory scopeFactory,
		ILogger<MultimodalVisionService> logger)
	{
		_logger = logger;
		_scopeFactory = scopeFactory;
	}

	// -----------------------------
	// NEW: REQUIRED BY INTERFACE
	// -----------------------------
	public async Task InitializeAsync(CancellationToken ct = default)
	{
		if (IsReady)
			return;

		_logger.LogInformation("Initializing multimodal vision service…");

		ModelTemplateDto? template;

		using (var scope = _scopeFactory.CreateScope())
		{
			var templates = scope.ServiceProvider.GetRequiredService<IModelConfigurationTemplateService>();
			template = await templates.GetDefaultTemplateAsync(ct);
		}

		var modelPath = template?.Models?.Vision?.LocalPath;

		if (string.IsNullOrWhiteSpace(modelPath))
		{
			_logger.LogWarning("Vision model path not configured.");
			return;
		}

		if (!Directory.Exists(modelPath))
		{
			_logger.LogWarning("Vision model directory does not exist: {Path}", modelPath);
			return;
		}

		try
		{
			_logger.LogInformation("Loading vision model from {Path}", modelPath);

			_model = new Model(modelPath);
			_processor = new MultiModalProcessor(_model);
			_tokenizer = new Tokenizer(_model);

			_logger.LogInformation("Vision model loaded successfully.");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to initialize multimodal vision model.");
			await DisposeAsync();
		}
	}

	// -----------------------------
	//     INFERENCE METHODS
	// -----------------------------

	public async Task<string> AnalyzeImageAsync(string prompt, byte[] imageBytes, CancellationToken ct = default)
	{
		if (!IsReady)
			throw new InvalidOperationException("Vision service not initialized.");

		await _semaphore.WaitAsync(ct);
		try
		{
			return await Task.Run(() => GenerateInternal(prompt, imageBytes), ct);
		}
		finally
		{
			_semaphore.Release();
		}
	}

	public async IAsyncEnumerable<string> AnalyzeImageStreamingAsync(
		string prompt,
		byte[] imageBytes,
		[System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
	{
		if (!IsReady)
			throw new InvalidOperationException("Vision service not initialized.");

		await _semaphore.WaitAsync(ct);
		try
		{
			var ortImages = Images.Load(imageBytes);
			var formatted = FormatPrompt(prompt);

			var tensors = _processor!.ProcessImages(formatted, ortImages);

			using var stream = _processor.CreateStream();
			using var genParams = new GeneratorParams(_model!);
			genParams.SetSearchOption("max_length", 3072);

			using var generator = new Generator(_model!, genParams);
			generator.SetInputs(tensors);

			while (!generator.IsDone() && !ct.IsCancellationRequested)
			{
				generator.GenerateNextToken();
				var seq = generator.GetSequence(0)[^1];
				var token = stream.Decode(seq);
				if (!string.IsNullOrEmpty(token))
					yield return token;
			}
		}
		finally
		{
			_semaphore.Release();
		}
	}

	private string GenerateInternal(string prompt, byte[] imageBytes)
	{
		var ortImages = Images.Load(imageBytes);
		var formatted = FormatPrompt(prompt);

		var tensors = _processor!.ProcessImages(formatted, ortImages);

		using var stream = _processor.CreateStream();
		using var genParams = new GeneratorParams(_model!);
		genParams.SetSearchOption("max_length", 3072);

		using var generator = new Generator(_model!, genParams);
		generator.SetInputs(tensors);

		var sb = new StringBuilder();

		while (!generator.IsDone())
		{
			generator.GenerateNextToken();
			var seq = generator.GetSequence(0)[^1];
			sb.Append(stream.Decode(seq));
		}

		return sb.ToString();
	}

	private static string FormatPrompt(string userPrompt)
	{
		return $"""
		<|user|>
		<|image_1|>
		You are CAILE-Vision, a digital forensics and OSINT image analyst.

		ANALYZE THIS IMAGE:

		1. TYPE: What kind of image is this? (screenshot, document, chat, photo, etc.)

		2. TEXT EXTRACTION: Transcribe ALL visible text exactly.
		   - Preserve spelling, formatting, line breaks
		   - Note language(s) detected
		   - [illegible] for unreadable portions

		3. GEOLOCATION INDICATORS: Identify region-specific elements.
		   - Electrical outlets/plug types (Type A, B, C, G, etc.)
		   - Power socket design, voltage indicators
		   - Vehicle license plates (format, color scheme)
		   - Road signs (style, language, symbols)
		   - Street furniture (bollards, crosswalk style, traffic lights)
		   - Architecture style, building materials, roof types
		   - Vegetation, landscape features
		   - Currency, price formats, units of measurement
		   - Language on signage, brands, products
		   - Clothing styles, uniforms
		   - Sun position/shadows (if relevant)
		   - Phone number formats, country codes
		   - Date formats (MM/DD vs DD/MM)
		   - Driving side (left/right) if visible

		4. DIGITAL IDENTIFIERS:
		   - Timestamps, dates, times, timezones
		   - Usernames, emails, phone numbers
		   - URLs, file paths, IP addresses
		   - Application names, OS indicators
		   - Device metadata visible in UI
		   - Reference numbers, case IDs

		5. OBJECTS & CONTEXT:
		   - Devices (make/model if identifiable)
		   - Documents (type, visible letterhead)
		   - People (count only, no identification)
		   - Weapons, contraband, evidence items

		6. ANALYST NOTES:
		   - Flag investigatively significant details
		   - [UNCERTAIN] for unclear observations
		   - [GEOLOC] for geolocation-relevant findings

		RULES:
		- Report ONLY what is directly visible
		- Do NOT identify individuals
		- Do NOT speculate on intent or context
		- BE SPECIFIC about shapes, colors, styles

		ANALYST QUESTION: {userPrompt}
		<|end|>
		<|assistant|>
		""";
	}


	// -----------------------------
	//     DISPOSAL
	// -----------------------------
	public void Dispose()
	{
		if (_disposed) return;

		_disposed = true;

		_semaphore.Dispose();
		_processor?.Dispose();
		_tokenizer?.Dispose();
		_model?.Dispose();

		_logger.LogInformation("Multimodal Vision Service disposed.");
	}

	public async ValueTask DisposeAsync()
	{
		Dispose();
		await Task.CompletedTask;
	}
}

public sealed class NullMultimodalVisionService : IMultimodalVisionService
{
	public bool IsReady => false;

	public Task InitializeAsync(CancellationToken ct = default)
		=> Task.CompletedTask;

	public Task<string> AnalyzeImageAsync(string prompt, byte[] imageBytes, CancellationToken ct = default)
		=> Task.FromResult("[Vision service not configured]");

	public async IAsyncEnumerable<string> AnalyzeImageStreamingAsync(
		string prompt,
		byte[] imageBytes,
		[System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
	{
		yield return "[Vision service not configured]";
		await Task.CompletedTask;
	}
}
