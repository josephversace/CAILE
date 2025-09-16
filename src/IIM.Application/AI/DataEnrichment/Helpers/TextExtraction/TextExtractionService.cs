using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Application.AI.DataEnrichment.Helpers
{
    /// <summary>
    /// Main text extraction service that delegates to format-specific extractors
    /// </summary>
    public class TextExtractionService : ITextExtractionService
    {
        private readonly ILogger<TextExtractionService> _logger;
        private readonly Dictionary<string, Func<Stream, CancellationToken, Task<string>>> _extractors;

        public TextExtractionService(ILogger<TextExtractionService> logger)
        {
            _logger = logger;
            _extractors = new Dictionary<string, Func<Stream, CancellationToken, Task<string>>>
            {
                ["text/plain"] = ExtractPlainTextAsync,
                ["application/pdf"] = ExtractPdfTextAsync,
                ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"] = ExtractWordTextAsync,
                ["application/json"] = ExtractJsonTextAsync,
                ["application/xml"] = ExtractXmlTextAsync,
                ["text/xml"] = ExtractXmlTextAsync
            };
        }

        public async Task<string> ExtractTextAsync(Stream content, string mimeType, CancellationToken cancellationToken = default)
        {
            content.Position = 0;

            try
            {
                if (_extractors.TryGetValue(mimeType.ToLower(), out var extractor))
                {
                    return await extractor(content, cancellationToken);
                }

                _logger.LogWarning("No text extractor available for MIME type {MimeType}", mimeType);
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract text content from {MimeType}", mimeType);
                return string.Empty;
            }
        }

        public bool SupportsFormat(string mimeType)
        {
            return _extractors.ContainsKey(mimeType.ToLower());
        }

        #region Format-Specific Extractors

        private async Task<string> ExtractPlainTextAsync(Stream content, CancellationToken cancellationToken)
        {
            using var reader = new StreamReader(content, leaveOpen: true);
            return await reader.ReadToEndAsync(cancellationToken);
        }

        private async Task<string> ExtractPdfTextAsync(Stream content, CancellationToken cancellationToken)
        {
            // TODO: Implement PDF text extraction using PdfPig or similar
            _logger.LogDebug("PDF text extraction not yet implemented");
            return "PDF text extraction not implemented";
        }

        private async Task<string> ExtractWordTextAsync(Stream content, CancellationToken cancellationToken)
        {
            // TODO: Implement Word document extraction using DocumentFormat.OpenXml
            _logger.LogDebug("Word document text extraction not yet implemented");
            return "Word document text extraction not implemented";
        }

        private async Task<string> ExtractJsonTextAsync(Stream content, CancellationToken cancellationToken)
        {
            using var reader = new StreamReader(content, leaveOpen: true);
            return await reader.ReadToEndAsync(cancellationToken);
        }

        private async Task<string> ExtractXmlTextAsync(Stream content, CancellationToken cancellationToken)
        {
            using var reader = new StreamReader(content, leaveOpen: true);
            return await reader.ReadToEndAsync(cancellationToken);
        }

   

        #endregion
    }
}