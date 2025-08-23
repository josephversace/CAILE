using IIM.Core.Mediator;
using IIM.Shared.DTOs;
using IIM.Shared.Enums;
using IIM.Shared.Models;
using System.ComponentModel.DataAnnotations;

namespace IIM.Application.Commands.Investigation
{
    /// <summary>
    /// Command to export an investigation response in various formats.
    /// Supports PDF, Word, Excel, JSON, CSV, and Markdown.
    /// </summary>
    public class ExportResponseCommand : IRequest<byte[]>
    {
        /// <summary>
        /// Gets the ID of the response to export.
        /// </summary>
        [Required]
        public string ResponseId { get; }

        /// <summary>
        /// Gets the export format.
        /// </summary>
        [Required]
        public ExportFormat Format { get; }

        /// <summary>
        /// Gets optional export configuration options.
        /// </summary>
        public ExportOptions? Options { get; }

        /// <summary>
        /// Gets the user ID requesting the export.
        /// </summary>
        public string? UserId { get; }

        /// <summary>
        /// Initializes a new instance of the ExportResponseCommand.
        /// </summary>
        /// <param name="responseId">ID of response to export</param>
        /// <param name="format">Export format</param>
        /// <param name="options">Optional export configuration</param>
        public ExportResponseCommand(string responseId, ExportFormat format, ExportOptions? options = null)
        {
            ResponseId = responseId ?? throw new ArgumentNullException(nameof(responseId));
            Format = format;
            Options = options;
        }
    }
}