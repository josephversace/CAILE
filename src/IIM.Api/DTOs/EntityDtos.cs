namespace IIM.Api.DTOs;

public record EntityDto(
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

public record RelationshipDto(
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
    List<EntityDto> Entities,
    int TotalCount,
    int Page,
    int PageSize
);