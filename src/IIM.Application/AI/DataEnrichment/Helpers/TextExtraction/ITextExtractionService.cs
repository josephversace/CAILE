using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Application.AI.DataEnrichment.Helpers
{
    /// <summary>
    /// Service for extracting text content from various file formats
    /// </summary>
    public interface ITextExtractionService
    {
        Task<string> ExtractTextAsync(Stream content, string mimeType, CancellationToken cancellationToken = default);
        bool SupportsFormat(string mimeType);
    }
}