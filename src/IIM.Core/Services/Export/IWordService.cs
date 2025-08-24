
using System;
using System.Threading.Tasks;

namespace IIM.Core.Services;

public interface IWordService
{
    Task<byte[]> GenerateDocumentAsync(object data, ExportOptions options);
    Task<byte[]> GenerateFromTemplateAsync(string templatePath, object model);
    Task<byte[]> ConvertToDocxAsync(string html);
    Task<byte[]> AddHeaderFooterAsync(byte[] docx, string header, string footer);
}
