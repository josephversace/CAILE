using IIM.Core.Mediator;
using IIM.Core.Models;
using IIM.Shared.Models;


namespace IIM.Application.Files
{
    /// <summary>
    /// Command to ingest file with chain of custody tracking
    /// </summary>
    public class IngestFileCommand : IRequest<FileContext>
    {
        public Stream FileStream { get; set; }
        public string FileName { get; set; }
        public FileMetadata Metadata { get; set; }
    }
}