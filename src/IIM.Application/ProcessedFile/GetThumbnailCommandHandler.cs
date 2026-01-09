using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Blake3;
using IIM.Shared.Interfaces;
using IIM.Shared.Mediator;
using IIM.Shared.Models;
using NPOI.OpenXmlFormats.Dml;
using SkiaSharp;

namespace IIM.Application.ProcessedFile;

public class GetThumbnailCommandHandler : IRequestHandler<GetThumbnailCommand, string>
{
	private readonly IWorkspaceManager _workspace;
	private readonly IFileStore _fileStore;
	private readonly IAuditService _audit;

	public GetThumbnailCommandHandler(IWorkspaceManager workspace, IFileStore fileStore, IAuditService audit)
	{
		_workspace = workspace;
		_fileStore = fileStore;
		_audit = audit; 
	}

	public async Task<string> Handle(GetThumbnailCommand request, CancellationToken cancellationToken)
	{
		if (string.IsNullOrEmpty(request.StoredFiledHash))
			throw new ArgumentException("FileId cannot be null or empty.", nameof(request.StoredFiledHash));

		// Check if thumbnail exists
		var thumbnailsJson = await _workspace.GetMetadataJsonAsync(
			request.StoredFiledHash,
			"ThumbnailGenerator",
			true);

		if (thumbnailsJson is not null && thumbnailsJson.Any())
		{
			var thumbnails = thumbnailsJson[0];
			var cached = JsonSerializer.Deserialize<Dictionary<string, string>>(thumbnails);

			if (cached != null)
			{
				return request.Size switch
				{
					ThumbnailSize.Small => cached["small"],
					ThumbnailSize.Medium => cached["medium"],
					ThumbnailSize.Large => cached["large"],
					_ => throw new ArgumentOutOfRangeException(nameof(request.Size))
				};
			}
		}

		// Generate thumbnails
		var fileMetadata = await _workspace.GetStoredFileByHashAsync(request.StoredFiledHash);

	
		if (fileMetadata is null)
			throw new FileNotFoundException("File not found.", request.StoredFiledHash);

		if (!fileMetadata.MimeType.StartsWith("image/"))
			throw new InvalidOperationException("File is not an image.");

		var bytes = await _fileStore.ReadAsync(fileMetadata.Bucket, request.StoredFiledHash);

		var base64Small = Convert.ToBase64String(CreateThumbnail(bytes, 250));
		var base64Medium = Convert.ToBase64String(CreateThumbnail(bytes, 500));
		var base64Large = Convert.ToBase64String(CreateThumbnail(bytes, 1024));

		var thumbnailMetadata = new Dictionary<string, string>
		{
			{ "small", base64Small },
			{ "medium", base64Medium },
			{ "large", base64Large }
		};

		var ordered = thumbnailMetadata
	.OrderBy(kvp => kvp.Key)
	.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

		byte[] derivedBytes = JsonSerializer.SerializeToUtf8Bytes(ordered);

		using var hasher = new Blake3HashAlgorithm();
		string derivedHash = Convert
			.ToHexString(hasher.ComputeHash(derivedBytes))
			.ToLowerInvariant();



		var processedFile = new Shared.Models.ProcessedFile
		{
			ProcessorName = "ThumbnailGenerator",
			StoredFileHash = request.StoredFiledHash,
			DerivedHash = derivedHash,
			MetadataJson = JsonSerializer.Serialize(thumbnailMetadata),
			ProcessorKind = "image"
		};

		await _workspace.AddProcessedFileAsync(processedFile);
		

		return request.Size switch
		{
			ThumbnailSize.Small => base64Small,
			ThumbnailSize.Medium => base64Medium,
			ThumbnailSize.Large => base64Large,
			_ => throw new ArgumentOutOfRangeException(nameof(request.Size))
		};
	}

	private static byte[] CreateThumbnail(byte[] imageBytes, int maxSize)
	{
		using var original = SKBitmap.Decode(imageBytes);

		if (original == null)
			throw new InvalidOperationException("Failed to decode image.");

		float scale = Math.Min(
			(float)maxSize / original.Width,
			(float)maxSize / original.Height);

		int newWidth = (int)(original.Width * scale);
		int newHeight = (int)(original.Height * scale);

		using var resized = original.Resize(
			new SKImageInfo(newWidth, newHeight),
			SKFilterQuality.High);

		if (resized == null)
			throw new InvalidOperationException("Failed to resize image.");

		using var image = SKImage.FromBitmap(resized);
		using var data = image.Encode(SKEncodedImageFormat.Jpeg, 85);

		return data.ToArray();
	}
}