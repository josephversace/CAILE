using IIM.Shared.DTOs;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using IIM.Shared.Models.Audit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Minio.Exceptions;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IIM.Api.Endpoints;

public static class AuditEndpoints
{
    public static void MapAuditEndpoints(this IEndpointRouteBuilder app)
    {
        var audit = app.MapGroup("/api/audit")
            .RequireAuthorization();

        // Get audit logs with filtering
        audit.MapGet("/logs", async (
            [FromServices] IAuditLogger auditLogger,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string? entityId,
            [FromQuery] string? userId,
            [FromQuery] string? eventType,
            [FromQuery] string? action,
            [FromQuery] int limit = 100,
            [FromQuery] int offset = 0) =>
        {
            // Map query parameters to filter
            var filter = new AuditLogFilter
            {
                StartDate = startDate,
                EndDate = endDate,
                EntityId = entityId,
                UserId = userId,
                EventType = eventType,
                Action = action,
                Limit = limit,
                Offset = offset
            };

            var logs = await auditLogger.GetAuditLogsAsync(filter);

            // Map to DTOs
            var response = new AuditLogResponse(
                Logs: logs.Select(log => new AuditEventDto(
                    Id: log.Id,
                    EventType: log.EventType,
                    UserId: log.UserId,
                    EntityId: log.EntityId,
                    EntityType: log.EntityType,
                    Action: log.Action,
                    Timestamp: log.Timestamp,
                    Success: log.Success,
                    Details: log.Details
                )).ToList(),
                TotalCount: logs.Count,
                HasMore: logs.Count >= limit
            );

            return Results.Ok(response);
        })
        .WithName("GetAuditLogs")
        .RequireAuthorization("AdminOnly")
        .Produces<AuditLogResponse>(200);

        // Get audit log by ID
        audit.MapGet("/logs/{id}", async (
            string id,
            [FromServices] IAuditLogger auditLogger) =>
        {
            var log = await auditLogger.GetAuditLogByIdAsync(id);

            if (log == null)
            {
                return Results.NotFound(new ErrorResponse(
                    ErrorCode: "AUDIT_LOG_NOT_FOUND",
                    Message: $"Audit log {id} not found"
                ));
            }

            // Map to DTO with full details
            var response = new AuditEventDetailDto(
                Id: log.Id,
                EventType: log.EventType,
                UserId: log.UserId,
                EntityId: log.EntityId,
                EntityType: log.EntityType,
                Action: log.Action,
                Details: log.Details,
                IpAddress: log.IpAddress,
                UserAgent: log.UserAgent,
                Timestamp: log.Timestamp,
                Success: log.Success,
                ErrorMessage: log.ErrorMessage,
                AdditionalData: log.AdditionalData
            );

            return Results.Ok(response);
        })
        .WithName("GetAuditLog")
        .RequireAuthorization("AdminOnly")
        .Produces<AuditEventDetailDto>(200)
        .Produces<ErrorResponse>(404);

        // Get audit logs for a specific entity
        audit.MapGet("/entity/{entityType}/{entityId}", async (
            string entityType,
            string entityId,
            [FromServices] IAuditLogger auditLogger,
            [FromQuery] int limit = 50) =>
        {
            var filter = new AuditLogFilter
            {
                EntityType = entityType,
                EntityId = entityId,
                Limit = limit,
                SortDescending = true
            };

            var logs = await auditLogger.GetAuditLogsAsync(filter);

            var response = new EntityAuditHistoryDto(
                EntityType: entityType,
                EntityId: entityId,
                Events: logs.Select(log => new AuditEventDto(
                    Id: log.Id,
                    EventType: log.EventType,
                    UserId: log.UserId,
                    EntityId: log.EntityId,
                    EntityType: log.EntityType,
                    Action: log.Action,
                    Timestamp: log.Timestamp,
                    Success: log.Success,
                    Details: log.Details
                )).ToList(),
                FirstActivity: logs.LastOrDefault()?.Timestamp,
                LastActivity: logs.FirstOrDefault()?.Timestamp,
                TotalEvents: logs.Count
            );

            return Results.Ok(response);
        })
        .WithName("GetEntityAuditHistory")
        .Produces<EntityAuditHistoryDto>(200);

        // Get audit statistics
        audit.MapGet("/stats", async (
            [FromServices] IAuditLogger auditLogger,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate) =>
        {
            var filter = new AuditLogFilter
            {
                StartDate = startDate ?? DateTime.UtcNow.AddDays(-30),
                EndDate = endDate ?? DateTime.UtcNow,
                Limit = 10000 // Get more for statistics
            };

            var logs = await auditLogger.GetAuditLogsAsync(filter);

            // Calculate statistics
            var stats = new AuditStatisticsDto(
                TotalEvents: logs.Count,
                SuccessfulEvents: logs.Count(l => l.Success),
                FailedEvents: logs.Count(l => !l.Success),
                UniqueUsers: logs.Select(l => l.UserId).Distinct().Count(),
                EventsByType: logs.GroupBy(l => l.EventType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                EventsByAction: logs.GroupBy(l => l.Action)
                    .ToDictionary(g => g.Key, g => g.Count()),
                MostActiveUsers: logs.GroupBy(l => l.UserId)
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .Select(g => new UserActivityDto(g.Key, g.Count()))
                    .ToList(),
                Period: new DateRangeDto(
                    filter.StartDate.Value,
                    filter.EndDate.Value
                )
            );

            return Results.Ok(stats);
        })
        .WithName("GetAuditStatistics")
        .RequireAuthorization("AdminOnly")
        .Produces<AuditStatisticsDto>(200);

        // Export audit logs
        audit.MapPost("/export", async (
            [FromBody] ExportAuditLogsRequest request,
            [FromServices] IAuditLogger auditLogger) =>
        {
            var filter = new AuditLogFilter
            {
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                EntityType = request.EntityType,
                UserId = request.UserId,
                EventType = request.EventType,
                Limit = request.MaxRecords ?? 10000
            };

            var logs = await auditLogger.GetAuditLogsAsync(filter);

            // TODO: Implement actual export logic based on format
            var exportData = request.Format?.ToLower() switch
            {
                "csv" => GenerateCsv(logs),
                "json" => GenerateJson(logs),
                _ => GenerateJson(logs)
            };

            return Results.Ok(new ExportAuditLogsResponse(
                FileName: $"audit_logs_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{request.Format ?? "json"}",
                Data: exportData,
                RecordCount: logs.Count,
                Format: request.Format ?? "json"
            ));
        })
        .WithName("ExportAuditLogs")
        .RequireAuthorization("AdminOnly")
        .Produces<ExportAuditLogsResponse>(200);
    }

    // Helper methods for export
    private static string GenerateCsv(List<AuditEvent> logs)
    {
        // Simple CSV generation (consider using a proper CSV library in production)
        var csv = "Id,EventType,UserId,EntityId,EntityType,Action,Timestamp,Success\n";
        foreach (var log in logs)
        {
            csv += $"{log.Id},{log.EventType},{log.UserId},{log.EntityId}," +
                   $"{log.EntityType},{log.Action},{log.Timestamp:O},{log.Success}\n";
        }
        return csv;
    }

    private static string GenerateJson(List<AuditEvent> logs)
    {
        return System.Text.Json.JsonSerializer.Serialize(logs, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
}