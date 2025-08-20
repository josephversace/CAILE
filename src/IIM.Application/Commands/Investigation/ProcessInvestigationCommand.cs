using IIM.Core.Mediator;
using IIM.Core.Models;
using IIM.Shared.Enums;
using IIM.Shared.Models;
using System;
using System.Collections.Generic;

namespace IIM.Application.Commands.Investigation
{
    /// <summary>
    /// Command to process an investigation query
    /// </summary>
    public class ProcessInvestigationCommand : IRequest<InvestigationResponse>
    {
        /// <summary>
        /// Session ID for the investigation
        /// </summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// The user's query text
        /// </summary>
        public string Query { get; set; } = string.Empty;

        /// <summary>
        /// Type of investigation
        /// </summary>
        public InvestigationType Type { get; set; } = InvestigationType.GeneralInquiry;

        /// <summary>
        /// List of tools enabled for this query
        /// </summary>
        public List<string> EnabledTools { get; set; } = new();

        /// <summary>
        /// Additional context for the query
        /// </summary>
        public Dictionary<string, object> Context { get; set; } = new();

        /// <summary>
        /// File attachments for the query
        /// </summary>
        public List<Attachment> Attachments { get; set; } = new();

        /// <summary>
        /// Maximum tokens for response
        /// </summary>
        public int MaxTokens { get; set; } = 2048;

        /// <summary>
        /// Temperature for generation (0.0 - 1.0)
        /// </summary>
        public double Temperature { get; set; } = 0.3;

        /// <summary>
        /// Whether to include citations
        /// </summary>
        public bool IncludeCitations { get; set; } = true;

        /// <summary>
        /// Whether to verify factual accuracy
        /// </summary>
        public bool VerifyAccuracy { get; set; } = true;

        /// <summary>
        /// Model configuration to use
        /// </summary>
        public string? ModelConfigurationId { get; set; }
    }
}