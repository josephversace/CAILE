using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Application.AI.DataEnrichment.Helpers
{
    /// <summary>
    /// Helper class for extracting technical metadata from files
    /// </summary>
    public class MetadataExtractor
    {
        private readonly ILogger<MetadataExtractor> _logger;

        public MetadataExtractor(ILogger<MetadataExtractor> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Extracts technical metadata from file stream
        /// </summary>
        public async Task<Dictionary<string, object>> ExtractAsync(Stream content, string fileName, string mimeType, CancellationToken cancellationToken = default)
        {
            var metadata = new Dictionary<string, object>
            {
                ["FileName"] = fileName,
                ["MimeType"] = mimeType,
                ["FileSize"] = content.Length,
                ["ExtractedAt"] = DateTime.UtcNow,
                ["FileExtension"] = Path.GetExtension(fileName)
            };

            try
            {
                content.Position = 0;

                // Basic file analysis
                var buffer = new byte[Math.Min(1024, content.Length)];
                await content.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                metadata["HasBinaryContent"] = buffer.Any(b => b < 32 && b != 9 && b != 10 && b != 13);
                metadata["FirstBytesSignature"] = Convert.ToHexString(buffer.Take(8).ToArray());

                // Add format-specific metadata extraction here
                await ExtractFormatSpecificMetadataAsync(content, mimeType, metadata, cancellationToken);

                _logger.LogDebug("Extracted technical metadata for file {FileName}", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error extracting metadata for file {FileName}", fileName);
            }

            return metadata;
        }


        private async Task ExtractPdfMetadataAsync(Stream content, Dictionary<string, object> metadata, CancellationToken cancellationToken)
        {
            // TODO: Implement PDF metadata extraction
            metadata["PdfMetadataExtracted"] = false;
        }

        private async Task ExtractImageMetadataAsync(Stream content, Dictionary<string, object> metadata, CancellationToken cancellationToken)
        {
            // TODO: Implement image metadata extraction (EXIF, etc.)
            metadata["ImageMetadataExtracted"] = false;
        }

        private async Task ExtractFormatSpecificMetadataAsync(Stream content, string mimeType,
           Dictionary<string, object> metadata, CancellationToken cancellationToken)
        {
            content.Position = 0;

            switch (mimeType.ToLower())
            {
                case "application/pdf":
                    await ExtractPdfMetadataAsync(content, metadata, cancellationToken);
                    break;
                case "application/vnd.openxmlformats-officedocument.wordprocessingml.document":
                    await ExtractWordMetadataAsync(content, metadata, cancellationToken);
                    break;
                case "image/jpeg":
                case "image/png":
                case "image/gif":
                    await ExtractImageMetadataAsync(content, metadata, cancellationToken);
                    break;
                case "application/json":
                    await ExtractJsonMetadataAsync(content, metadata, cancellationToken);
                    break;
                default:
                    _logger.LogDebug("No specific metadata extractor for MIME type {MimeType}", mimeType);
                    break;
            }
        }


        private async Task ExtractWordMetadataAsync(Stream content, Dictionary<string, object> metadata, CancellationToken cancellationToken)
        {
            // TODO: Implement Word document metadata extraction
            metadata["EstimatedWordCount"] = "Unknown";
            metadata["DocumentFormat"] = "DOCX";
        }


        private async Task ExtractJsonMetadataAsync(Stream content, Dictionary<string, object> metadata, CancellationToken cancellationToken)
        {
            try
            {
                content.Position = 0;
                using var reader = new StreamReader(content, leaveOpen: true);
                var jsonContent = await reader.ReadToEndAsync(cancellationToken);

                var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonContent);
                var elementCount = CountJsonElements(jsonDoc.RootElement);

                metadata["JsonElementCount"] = elementCount;
                metadata["JsonRootType"] = jsonDoc.RootElement.ValueKind.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract JSON metadata");
                metadata["JsonParseError"] = ex.Message;
            }
        }

        private int CountJsonElements(System.Text.Json.JsonElement element)
        {
            int count = 1;

            switch (element.ValueKind)
            {
                case System.Text.Json.JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                    {
                        count += CountJsonElements(property.Value);
                    }
                    break;
                case System.Text.Json.JsonValueKind.Array:
                    foreach (var item in element.EnumerateArray())
                    {
                        count += CountJsonElements(item);
                    }
                    break;
            }

            return count;
        }
    }
}