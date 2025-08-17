using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using IIM.Shared.Enums;

namespace IIM.Core.Models;


public class Entity
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public EntityType Type { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
    public List<string> Aliases { get; set; } = new();
    public List<Relationship> Relationships { get; set; } = new();
    public List<string> AssociatedCaseIds { get; set; } = new();
    public double RiskScore { get; set; }
    public DateTimeOffset FirstSeen { get; set; }
    public DateTimeOffset LastSeen { get; set; }
    public Dictionary<string, object> Attributes { get; set; } = new();
}

public class Relationship
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string SourceEntityId { get; set; } = string.Empty;
    public string TargetEntityId { get; set; } = string.Empty;
    public RelationshipType Type { get; set; }
    public double Strength { get; set; }
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
}





