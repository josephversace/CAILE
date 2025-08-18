using System;
using System.IO;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using IIM.Core.Models;
using IIM.Shared.DTOs;
using Microsoft.Extensions.Logging;

namespace IIM.Core.Services;

public class WordService : IWordService
{
    private readonly ILogger<WordService> _logger;
    
    public WordService(ILogger<WordService> logger)
    {
        _logger = logger;
    }
    
    public async Task<byte[]> GenerateDocumentAsync(object data, ExportOptions options)
    {
        using var memoryStream = new MemoryStream();
        using (var wordDocument = WordprocessingDocument.Create(memoryStream, WordprocessingDocumentType.Document))
        {
            // Add main document part
            var mainPart = wordDocument.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());
            
            // Add title
            if (options.IncludeHeaders)
            {
                var titleParagraph = body.AppendChild(new Paragraph());
                var titleRun = titleParagraph.AppendChild(new Run());
                titleRun.AppendChild(new Text("Investigation Response"));
                
                // Apply heading style
                titleParagraph.ParagraphProperties = new ParagraphProperties(
                    new ParagraphStyleId() { Val = "Heading1" });
            }
            
            // Add content based on data type
            if (data is InvestigationResponse response)
            {
                AddResponseContent(body, response, options);
            }
            
            // Add metadata if requested
            if (options.IncludeMetadata)
            {
                AddMetadata(wordDocument, data);
            }
            
            mainPart.Document.Save();
        }
        
        return memoryStream.ToArray();
    }
    
    public async Task<byte[]> GenerateFromTemplateAsync(string templatePath, object model)
    {
        // Load template and replace placeholders
        var templateBytes = await File.ReadAllBytesAsync(templatePath);
        using var memoryStream = new MemoryStream();
        memoryStream.Write(templateBytes, 0, templateBytes.Length);
        
        using (var wordDocument = WordprocessingDocument.Open(memoryStream, true))
        {
            // Replace placeholders in the document
            var body = wordDocument.MainDocumentPart.Document.Body;
            foreach (var text in body.Descendants<Text>())
            {
                // Simple placeholder replacement - in production, use a proper template engine
                if (text.Text.Contains("{{") && text.Text.Contains("}}"))
                {
                    // Replace placeholders with actual values
                    text.Text = ReplacePlaceholders(text.Text, model);
                }
            }
            
            wordDocument.MainDocumentPart.Document.Save();
        }
        
        return memoryStream.ToArray();
    }
    
    public async Task<byte[]> ConvertToDocxAsync(string html)
    {
        // For full HTML to DOCX conversion, you'd typically use a library like HtmlToOpenXml
        throw new NotImplementedException("HTML to DOCX conversion requires additional libraries");
    }
    
    public async Task<byte[]> AddHeaderFooterAsync(byte[] docx, string header, string footer)
    {
        using var memoryStream = new MemoryStream(docx);
        using (var wordDocument = WordprocessingDocument.Open(memoryStream, true))
        {
            // Add header
            var mainPart = wordDocument.MainDocumentPart;
            var headerPart = mainPart.AddNewPart<HeaderPart>();
            headerPart.Header = new Header(new Paragraph(new Run(new Text(header))));
            
            // Add footer
            var footerPart = mainPart.AddNewPart<FooterPart>();
            footerPart.Footer = new Footer(new Paragraph(new Run(new Text(footer))));
            
            wordDocument.MainDocumentPart.Document.Save();
        }
        
        return memoryStream.ToArray();
    }
    
    private void AddResponseContent(Body body, InvestigationResponse response, ExportOptions options)
    {
        // Add response content
        var contentParagraph = body.AppendChild(new Paragraph());
        var contentRun = contentParagraph.AppendChild(new Run());
        contentRun.AppendChild(new Text(response.Content));
        
        // Add confidence if available
        if (response.Confidence.HasValue && options.IncludeMetadata)
        {
            var confidenceParagraph = body.AppendChild(new Paragraph());
            var confidenceRun = confidenceParagraph.AppendChild(new Run());
            confidenceRun.AppendChild(new Text($"Confidence: {response.Confidence:P0}"));
        }
    }
    
    private void AddMetadata(WordprocessingDocument document, object data)
    {
        var props = document.PackageProperties;
        props.Creator = "IIM Platform";
        props.Created = DateTime.UtcNow;
        props.Title = "Investigation Response Export";
        props.Subject = "Law Enforcement Investigation";
    }
    
    private string ReplacePlaceholders(string text, object model)
    {
        // Simple implementation - in production, use reflection or a proper template engine
        return text;
    }
}
