
using System;
using System.Collections.Generic;

namespace IIM.Shared.DTOs;

public record Entity(
    string Id,
    string Name,
    string Type,
    Dictionary<string, object> Properties,
    List<string>? Aliases,
    List<RelationshipDto>? Relationships,
    List<string>? AssociatedCaseIds,
    double RiskScore,
    DateTimeOffset FirstSeen,
    DateTimeOffset LastSeen,
    Dictionary<string, object>? Attributes
);

public record Relationship(
    string Id,
    string SourceEntityId,
    string TargetEntityId,
    string Type,
    double Strength,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate,
    Dictionary<string, object>? Properties
);

public record EntityListResponse(
    List<Entity> Entities,
    int TotalCount,
    int Page,
    int PageSize
);