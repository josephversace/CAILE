using IIM.Core.Mediator;
using IIM.Core.Models;
using IIM.Shared.Models;


namespace IIM.Application.Commands.Evidence
{
    /// <summary>
    /// Command to ingest evidence with chain of custody tracking
    /// </summary>
    public class IngestEvidenceCommand : IRequest<EvidenceContext>
    {
        public Stream FileStream { get; set; }
        public string FileName { get; set; }
        public EvidenceMetadata Metadata { get; set; }
    }
}