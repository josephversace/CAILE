using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IIM.Core.Services;

public interface IPdfService
{
    Task<byte[]> GeneratePdfAsync(string html, PdfGenerationOptions options);
    Task<byte[]> GeneratePdfFromTemplateAsync(string templateName, object model, PdfGenerationOptions options);
    Task<byte[]> MergePdfsAsync(List<byte[]> pdfs);
    Task<byte[]> AddWatermarkAsync(byte[] pdf, string watermark);
    Task<PdfMetadata> GetPdfMetadataAsync(byte[] pdf);
}

public class PdfGenerationOptions
{
    public PageSize PageSize { get; set; } = PageSize.Letter;
    public Margins Margins { get; set; } = new Margins(0.75f, 0.75f, 0.75f, 0.75f);
    public bool IncludeHeaders { get; set; } = true;
    public bool IncludeFooters { get; set; } = true;
    public string? HeaderHtml { get; set; }
    public string? FooterHtml { get; set; }
    public bool Landscape { get; set; } = false;
    public int? Dpi { get; set; } = 300;
}

public enum PageSize
{
    Letter,
    Legal,
    A4,
    A3
}

public class Margins
{
    public float Top { get; set; }
    public float Right { get; set; }
    public float Bottom { get; set; }
    public float Left { get; set; }
    
    public Margins(float top, float right, float bottom, float left)
    {
        Top = top;
        Right = right;
        Bottom = bottom;
        Left = left;
    }
}

public class PdfMetadata
{
    public int PageCount { get; set; }
    public string? Title { get; set; }
    public string? Author { get; set; }
    public DateTime? CreatedDate { get; set; }
    public long FileSize { get; set; }
}
