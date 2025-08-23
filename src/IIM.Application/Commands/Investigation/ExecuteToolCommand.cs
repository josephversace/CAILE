using IIM.Core.Mediator;
using IIM.Shared.DTOs;
using System.ComponentModel.DataAnnotations;
using IIM.Shared.Models;

namespace IIM.Application.Commands.Investigation
{
    /// <summary>
    /// Command to execute a specific tool within an investigation session.
    /// Tools include image search, transcription, document search, etc.
    /// </summary>
    public class ExecuteToolCommand : IRequest<ToolResult>
    {
        /// <summary>
        /// Gets the session ID where the tool is being executed.
        /// </summary>
        [Required]
        public string SessionId { get; }

        /// <summary>
        /// Gets the name of the tool to execute.
        /// </summary>
        [Required]
        public string ToolName { get; }

        /// <summary>
        /// Gets the parameters for tool execution.
        /// </summary>
        [Required]
        public Dictionary<string, object> Parameters { get; }

        /// <summary>
        /// Gets the optional case ID for context.
        /// </summary>
        public string? CaseId { get; }

        /// <summary>
        /// Gets the user ID executing the tool.
        /// </summary>
        public string? UserId { get; }

        /// <summary>
        /// Initializes a new instance of the ExecuteToolCommand.
        /// </summary>
        /// <param name="sessionId">Session ID where tool is executed</param>
        /// <param name="toolName">Name of the tool to execute</param>
        /// <param name="parameters">Parameters for the tool</param>
        public ExecuteToolCommand(string sessionId, string toolName, Dictionary<string, object> parameters)
        {
            SessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
            ToolName = toolName ?? throw new ArgumentNullException(nameof(toolName));
            Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
        }
    }
}