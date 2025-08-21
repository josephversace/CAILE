using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using IIM.Shared.Models;

namespace IIM.Infrastructure.Data.Entities
{
    /// <summary>
    /// Database entity for audit logs
    /// </summary>
    [Table("AuditLogs")]
    public class AuditLogEntity
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(100)]
        public string EventType { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? UserId { get; set; }

        [MaxLength(100)]
        public string? RequestId { get; set; }

        [MaxLength(100)]
        public string? ModelId { get; set; }

        [MaxLength(100)]
        public string? EntityId { get; set; }

        [MaxLength(50)]
        public string? EntityType { get; set; }

        [MaxLength(50)]
        public string? Action { get; set; }

        [MaxLength(50)]
        public string? Result { get; set; }

        public long? DurationMs { get; set; }

        [MaxLength(45)]
        public string? IpAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        // Store details as JSON
        public string? DetailsJson { get; set; }

        // Helper to convert to/from domain model
        public AuditLog ToDomainModel()
        {
            return new AuditLog
            {
                Id = Id,
                Timestamp = Timestamp,
                EventType = EventType,
                UserId = UserId,
                RequestId = RequestId,
                ModelId = ModelId,
                EntityId = EntityId,
                EntityType = EntityType,
                Action = Action,
                Result = Result,
                DurationMs = DurationMs,
                IpAddress = IpAddress,
                UserAgent = UserAgent,
                Details = string.IsNullOrEmpty(DetailsJson)
                    ? null
                    : JsonSerializer.Deserialize<Dictionary<string, object>>(DetailsJson)
            };
        }

        public static AuditLogEntity FromAuditEvent(AuditEvent evt)
        {
            var details = evt.AdditionalData as Dictionary<string, object> ?? new Dictionary<string, object>();

            if (evt.ErrorType != null)
                details["ErrorType"] = evt.ErrorType;
            if (evt.ErrorMessage != null)
                details["ErrorMessage"] = evt.ErrorMessage;
            if (evt.Priority != null)
                details["Priority"] = evt.Priority;
            if (evt.TokensGenerated.HasValue)
                details["TokensGenerated"] = evt.TokensGenerated.Value;

            return new AuditLogEntity
            {
                Timestamp = evt.Timestamp.UtcDateTime,
                EventType = evt.EventType,
                UserId = evt.UserId,
                RequestId = evt.RequestId,
                ModelId = evt.ModelId,
                EntityId = evt.EntityId,
                EntityType = evt.EntityType,
                Action = evt.Action,
                Result = evt.Result,
                DurationMs = evt.DurationMs,
                IpAddress = evt.IpAddress,
                UserAgent = evt.UserAgent,
                DetailsJson = details.Count > 0 ? JsonSerializer.Serialize(details) : null
            };
        }
    }
}
