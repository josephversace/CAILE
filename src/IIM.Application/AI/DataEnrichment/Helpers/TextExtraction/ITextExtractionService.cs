using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Application.AI.DataEnrichment.Helpers
{
    /// <summary>
    /// Interface for text extraction from various file formats
    /// </summary>
    public interface ITextExtractionService
    {
        /// <summary>
        /// Extracts text content from a stream based on MIME type
        /// </summary>
        Task<string> ExtractTextAsync(Stream content, string mimeType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if the service supports extracting text from the given format
        /// </summary>
        bool SupportsFormat(string mimeType);
    }
}