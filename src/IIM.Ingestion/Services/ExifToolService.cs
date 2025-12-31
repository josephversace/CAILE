using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IIM.Ingestion.Interfaces;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.ML.Tokenizers;

namespace IIM.Ingestion.Services;

public sealed class ExifToolService : IExifToolService
{
	private readonly ILogger<ExifToolService> _logger;
	private readonly CaileConfig _config;
	private readonly string _exifToolPath;
	private string? _cachedVersion;

	private readonly string _tempRoot;

	public ExifToolService(
		ILogger<ExifToolService> logger,
		CaileConfig config,
		string? exifToolPath = null,
		string? tempRoot = null)
	{
		_logger = logger;

		var exif = config.Tools?.ExifTool
				?? throw new InvalidOperationException("ExifTool config missing.");

		if (exif.Required && string.IsNullOrWhiteSpace(exif.Path))
			throw new InvalidOperationException("ExifTool path not configured.");

		if (exif.Required && !File.Exists(exif.Path))
			throw new FileNotFoundException("ExifTool executable not found.", exif.Path);

		_exifToolPath = exif.Path;
		_logger = logger;
		_tempRoot = tempRoot ?? Path.Combine(Path.GetTempPath(), "iim", "exif");
		Directory.CreateDirectory(_tempRoot);
	}

	public async Task<ExifToolResult?> RunAsync(
		byte[] bytes,
		string fileName,
		string blake3Hash,
		ExifToolProfile profile,
		CancellationToken ct)
	{
		var sw = Stopwatch.StartNew();
		var exifVersion = await GetExifToolVersionAsync(ct);

		var extension = Path.GetExtension(fileName);
		var tempFile = Path.Combine(_tempRoot, $"{blake3Hash}{extension}");

		try
		{
			await File.WriteAllBytesAsync(tempFile, bytes, ct);

			var args = BuildArguments(profile, tempFile);
			var psi = new ProcessStartInfo
			{
				FileName = _exifToolPath,
				Arguments = args,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true
			};

			using var proc = Process.Start(psi);
			if (proc is null)
				throw new InvalidOperationException("Failed to start ExifTool process");

			var stdoutTask = proc.StandardOutput.ReadToEndAsync();
			var stderrTask = proc.StandardError.ReadToEndAsync();

			await proc.WaitForExitAsync(ct);

			var stdout = await stdoutTask;
			var stderr = await stderrTask;

			if (proc.ExitCode != 0)
			{
				_logger.LogWarning(
					"ExifTool failed ({Code}) for {Hash}: {Error}",
					proc.ExitCode,
					blake3Hash[..12],
					stderr);
				return null;
			}

			if (string.IsNullOrWhiteSpace(stdout))
				return null;

			var json = JsonDocument.Parse(stdout);

			sw.Stop();

			var normalized = NormalizeInternal(json);

			return new ExifToolResult(
		profile,
		exifVersion,
		blake3Hash,
		fileName,
		json,
		normalized,
		DateTimeOffset.UtcNow,
		sw.Elapsed);


		}
		finally
		{
			TryDelete(tempFile);
		}
	}

	private static string BuildArguments(ExifToolProfile profile, string filePath)
	{
		var sb = new StringBuilder();
		sb.Append("-json -struct -charset utf8 --binary ");

		switch (profile)
		{
			case ExifToolProfile.Fast:
				sb.Append("-fast2 ");
				break;

			case ExifToolProfile.Deep:
				sb.Append("-api LargeFileSupport=1 -ExtractEmbedded=1 -ee ");
				break;

			case ExifToolProfile.MediaOnly:
				sb.Append("-Group0=EXIF,XMP,GPS,QuickTime ");
				break;
		}

		sb.Append('"').Append(filePath).Append('"');
		return sb.ToString();
	}

	private async Task<string> GetExifToolVersionAsync(CancellationToken ct)
	{
		if (_cachedVersion is not null)
			return _cachedVersion;

		var psi = new ProcessStartInfo
		{
			FileName = _exifToolPath,
			Arguments = "-ver",
			RedirectStandardOutput = true,
			UseShellExecute = false,
			CreateNoWindow = true
		};

		using var proc = Process.Start(psi)
			?? throw new InvalidOperationException("Failed to start ExifTool");

		var output = await proc.StandardOutput.ReadToEndAsync(ct);
		await proc.WaitForExitAsync(ct);

		_cachedVersion = output.Trim();
		return _cachedVersion;
	}

	public NormalizedExifMetadata Normalize(JsonDocument raw)
	{
		return NormalizeInternal(raw);
	}

	private static NormalizedExifMetadata NormalizeInternal(JsonDocument raw)
	{
		var root = raw.RootElement;
		if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
			return new NormalizedExifMetadata();

		var doc = root[0];

		return new NormalizedExifMetadata
		{
			Gps = ExtractGps(doc),
			Camera = ExtractCamera(doc),
			Software = ExtractSoftware(doc),
			Dates = ExtractDates(doc)
		};
	}

	private static ExifGpsMetadata? ExtractGps(JsonElement doc)
	{
		if (!TryGetGroup(doc, "GPS", out var gps))
			return null;

		if (!TryGetDouble(gps, "GPSLatitude", out var lat) ||
			!TryGetDouble(gps, "GPSLongitude", out var lon))
			return null;

		return new ExifGpsMetadata
		{
			Latitude = lat,
			Longitude = lon,
			AltitudeMeters =
				TryGetDouble(gps, "GPSAltitude", out var alt) ? alt : null,
			TimestampUtc =
				ParseDate(GetString(gps, "GPSDateTime"))
		};
	}

	private static ExifCameraMetadata? ExtractCamera(JsonElement doc)
	{
		if (!TryGetGroup(doc, "EXIF", out var exif))
			return null;

		return new ExifCameraMetadata
		{
			Make = GetString(exif, "Make"),
			Model = GetString(exif, "Model"),
			SerialNumber =
				GetString(exif, "BodySerialNumber") ??
				GetString(exif, "CameraSerialNumber"),
			Lens = GetString(exif, "LensModel")
		};
	}

	private static ExifSoftwareMetadata? ExtractSoftware(JsonElement doc)
	{
		TryGetGroup(doc, "XMP", out var xmp);
		TryGetGroup(doc, "File", out var file);

		var creator =
			GetString(xmp, "CreatorTool") ??
			GetString(xmp, "CreateTool");

		var software =
			GetString(doc, "Software") ??
			GetString(file, "Software");

		return creator is null && software is null
			? null
			: new ExifSoftwareMetadata
			{
				CreatorTool = creator,
				Software = software,
				OperatingSystem = GetString(xmp, "OperatingSystem")
			};
	}

	private static ExifDateMetadata? ExtractDates(JsonElement doc)
	{
		TryGetGroup(doc, "EXIF", out var exif);
		TryGetGroup(doc, "XMP", out var xmp);
		TryGetGroup(doc, "File", out var file);

		var dto = new ExifDateMetadata
		{
			DateTimeOriginal =
				ParseDate(GetString(exif, "DateTimeOriginal")),
			CreateDate =
				ParseDate(GetString(xmp, "CreateDate") ??
						  GetString(exif, "CreateDate")),
			ModifyDate =
				ParseDate(GetString(file, "ModifyDate") ??
						  GetString(xmp, "ModifyDate"))
		};

		return dto.DateTimeOriginal is null &&
			   dto.CreateDate is null &&
			   dto.ModifyDate is null
			? null
			: dto;
	}

	private static bool TryGetGroup(
	JsonElement root,
	string name,
	out JsonElement group)
	{
		if (root.TryGetProperty(name, out group))
			return true;

		group = default;
		return false;
	}

	private static string? GetString(JsonElement element, string name)
	{
		return element.ValueKind == JsonValueKind.Object &&
			   element.TryGetProperty(name, out var prop) &&
			   prop.ValueKind == JsonValueKind.String
			? prop.GetString()
			: null;
	}

	private static bool TryGetDouble(
		JsonElement element,
		string name,
		out double value)
	{
		value = default;

		if (!element.TryGetProperty(name, out var prop))
			return false;

		if (prop.ValueKind == JsonValueKind.Number &&
			prop.TryGetDouble(out value))
			return true;

		return prop.ValueKind == JsonValueKind.String &&
			   double.TryParse(prop.GetString(),
				   NumberStyles.Float,
				   CultureInfo.InvariantCulture,
				   out value);
	}

	private static DateTimeOffset? ParseDate(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return null;

		// Replace first two ':' only (YYYY:MM:DD → YYYY-MM-DD)
		value = value.Length >= 10
			? value.Substring(0, 10).Replace(':', '-') + value.Substring(10)
			: value.Replace(':', '-');


		return DateTimeOffset.TryParse(
			value,
			CultureInfo.InvariantCulture,
			DateTimeStyles.AssumeUniversal |
			DateTimeStyles.AdjustToUniversal,
			out var dto)
			? dto
			: null;
	}


	private void TryDelete(string path)
	{
		try
		{
			if (File.Exists(path))
				File.Delete(path);
		}
		catch (Exception ex)
		{
			_logger.LogDebug(ex, "Failed to delete temp ExifTool file {Path}", path);
		}
	}
}
