using System;
using System.Collections.Generic;
using System.Linq;
using IIM.Shared.Enums;

namespace IIM.Shared.Models
{
    /// <summary>
    /// Domain model representing a message in an investigation session
    /// </summary>
    public class InvestigationMessage
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string? SessionId { get; set; }
        public MessageRole Role { get; set; }
        public string Content { get; set; } = string.Empty;

        // Message Status
        public MessageStatus Status { get; set; } = MessageStatus.Processing;
        public double? Confidence { get; set; }

        // Attachments and Results
        public List<Attachment> Attachments { get; set; } = new();
        public List<ToolResult> ToolResults { get; set; } = new();
        public List<Citation> Citations { get; set; } = new();

        // Related Entities
        public List<string> EvidenceIds { get; set; } = new();
        public List<string> EntityIds { get; set; } = new();

        // Model Information
        public string? ModelUsed { get; set; }
        public string? FineTuneJobId { get; set; }

        // Threading
        public string? ParentMessageId { get; set; }
        public List<string> ChildMessageIds { get; set; } = new();

        // Timestamps
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? EditedAt { get; set; }
        public string? EditedBy { get; set; }

        // Metadata
        public Dictionary<string, object> Metadata { get; set; } = new();

        // Business Methods

        /// <summary>
        /// Adds an attachment to the message
        /// </summary>
        public void AddAttachment(Attachment attachment)
        {
            if (attachment == null)
                throw new ArgumentNullException(nameof(attachment));

            Attachments.Add(attachment);
        }

        /// <summary>
        /// Adds a tool result to the message
        /// </summary>
        public void AddToolResult(ToolResult result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            ToolResults.Add(result);

            // Update status if this is a tool message
            if (Role == MessageRole.Tool)
            {
                Status = result.Success ? MessageStatus.Completed : MessageStatus.Failed;
            }
        }

        /// <summary>
        /// Adds a citation to the message
        /// </summary>
        public void AddCitation(Citation citation)
        {
            if (citation == null)
                throw new ArgumentNullException(nameof(citation));

            Citations.Add(citation);
        }

        /// <summary>
        /// Links evidence to the message
        /// </summary>
        public void LinkEvidence(string evidenceId)
        {
            if (!EvidenceIds.Contains(evidenceId))
                EvidenceIds.Add(evidenceId);
        }

        /// <summary>
        /// Links an entity to the message
        /// </summary>
        public void LinkEntity(string entityId)
        {
            if (!EntityIds.Contains(entityId))
                EntityIds.Add(entityId);
        }

        /// <summary>
        /// Edits the message content
        /// </summary>
        public void Edit(string newContent, string editedBy)
        {
            if (string.IsNullOrWhiteSpace(newContent))
                throw new ArgumentException("Content cannot be empty");

            // Store original in metadata if first edit
            if (EditedAt == null)
            {
                Metadata["OriginalContent"] = Content;
            }

            Content = newContent;
            EditedAt = DateTimeOffset.UtcNow;
            EditedBy = editedBy;
            Status = MessageStatus.Edited;
        }

        /// <summary>
        /// Adds a reply to this message
        /// </summary>
        public void AddReply(string replyMessageId)
        {
            if (!ChildMessageIds.Contains(replyMessageId))
                ChildMessageIds.Add(replyMessageId);
        }

        /// <summary>
        /// Marks the message as processing
        /// </summary>
        public void MarkAsProcessing()
        {
            Status = MessageStatus.Processing;
        }

        /// <summary>
        /// Marks the message as completed
        /// </summary>
        public void MarkAsCompleted()
        {
            Status = MessageStatus.Completed;
        }

        /// <summary>
        /// Marks the message as failed
        /// </summary>
        public void MarkAsFailed(string errorMessage)
        {
            Status = MessageStatus.Failed;
            Metadata["ErrorMessage"] = errorMessage;
        }

        /// <summary>
        /// Checks if the message has results
        /// </summary>
        public bool HasResults()
        {
            return ToolResults.Any() || Citations.Any() || Attachments.Any();
        }

        /// <summary>
        /// Gets the message summary
        /// </summary>
        public string GetSummary(int maxLength = 100)
        {
            if (string.IsNullOrWhiteSpace(Content))
                return string.Empty;

            return Content.Length <= maxLength
                ? Content
                : Content.Substring(0, maxLength) + "...";
        }

        /// <summary>
        /// Checks if the message is a system message
        /// </summary>
        public bool IsSystemMessage()
        {
            return Role == MessageRole.System || Role == MessageRole.Tool;
        }
    }
}