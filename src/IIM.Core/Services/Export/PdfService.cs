using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace IIM.Core.Services;

public class PdfService : IPdfService
{
    private readonly ILogger<PdfService> _logger;
    private readonly ITemplateEngine _templateEngine;
    
    public PdfService(ILogger<PdfService> logger, ITemplateEngine templateEngine)
    {
        _logger = logger;
        _templateEngine = templateEngine;
    }
    
    public async Task<byte[]> GeneratePdfAsync(string html, PdfGenerationOptions options)
    {
        try
        {
            // Download Chromium if not already present
            await new BrowserFetcher().DownloadAsync();
            
            using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
            });
            
            using var page = await browser.NewPageAsync();
            await page.SetContentAsync(html);
            
            var pdfOptions = new PdfOptions
            {
                Format = GetPaperFormat(options.PageSize),
                Landscape = options.Landscape,
                MarginOptions = new MarginOptions
                {
                    Top = $"{options.Margins.Top}in",
                    Right = $"{options.Margins.Right}in",
                    Bottom = $"{options.Margins.Bottom}in",
                    Left = $"{options.Margins.Left}in"
                },
                DisplayHeaderFooter = options.IncludeHeaders || options.IncludeFooters,
                HeaderTemplate = options.HeaderHtml ?? string.Empty,
                FooterTemplate = options.FooterHtml ?? string.Empty,
                PrintBackground = true
            };
            
            return await page.PdfDataAsync(pdfOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate PDF");
            throw;
        }
    }
    
    public async Task<byte[]> GeneratePdfFromTemplateAsync(string templateName, object model, PdfGenerationOptions options)
    {
        var html = await _templateEngine.RenderTemplateAsync(templateName, model);
        return await GeneratePdfAsync(html, options);
    }
    
    public async Task<byte[]> MergePdfsAsync(List<byte[]> pdfs)
    {
        // Implementation using iTextSharp or similar
        throw new NotImplementedException("PDF merging requires iTextSharp or similar library");
    }
    
    public async Task<byte[]> AddWatermarkAsync(byte[] pdf, string watermark)
    {
        // Implementation using iTextSharp or similar
        throw new NotImplementedException("Watermarking requires iTextSharp or similar library");
    }
    
    public async Task<PdfMetadata> GetPdfMetadataAsync(byte[] pdf)
    {
        return new PdfMetadata
        {
            PageCount = 1, // Placeholder
            FileSize = pdf.Length,
            CreatedDate = DateTime.UtcNow
        };
    }
    
    private PaperFormat GetPaperFormat(PageSize size)
    {
        return size switch
        {
            PageSize.Letter => PaperFormat.Letter,
            PageSize.Legal => PaperFormat.Legal,
            PageSize.A4 => PaperFormat.A4,
            PageSize.A3 => PaperFormat.A3,
            _ => PaperFormat.Letter
        };
    }
}
