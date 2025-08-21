using IIM.Core.Mediator;
using IIM.Core.Models;
using IIM.Shared.Enums;
using IIM.Shared.Models;
using System.Collections.Generic;

namespace IIM.Application.Commands.Investigation
{
    /// <summary>
    /// Command to create a new investigation session
    /// </summary>
    public class CreateSessionCommand : IRequest<InvestigationSession>
    {
        /// <summary>
        /// Case ID this session belongs to
        /// </summary>
        public string CaseId { get; set; } = string.Empty;

        /// <summary>
        /// Title for the session
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Type of investigation
        /// </summary>
        public InvestigationType Type { get; set; } = InvestigationType.GeneralInquiry;

        /// <summary>
        /// Description of the investigation
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Tools to enable for this session
        /// </summary>
        public List<string> EnabledTools { get; set; } = new()
        {
            "rag_search",
            "image_analysis",
            "audio_transcription",
            "pattern_analysis"
        };

        /// <summary>
        /// Model configuration template to use
        /// </summary>
        public string? ModelTemplateId { get; set; }

        /// <summary>
        /// Initial context for the session
        /// </summary>
        public Dictionary<string, object>? InitialContext { get; set; }
    }
}