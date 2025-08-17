using System;

namespace IIM.Shared.Models;

/// <summary>
/// Represents a time range with helper methods
/// </summary>
public class TimeRange
{
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }
    
    public TimeSpan Duration => End - Start;
    
    public bool Contains(DateTimeOffset timestamp) => 
        timestamp >= Start && timestamp <= End;
}
