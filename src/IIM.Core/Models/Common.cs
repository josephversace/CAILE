
namespace IIM.Core.Models;

public class Attachment
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public AttachmentType Type { get; set; }
    public string? StoragePath { get; set; }
    public Stream? Stream { get; set; }
}

public class Citation
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string SourceId { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public int? PageNumber { get; set; }
    public string? Location { get; set; }
    public double Relevance { get; set; }
}

public class GeoLocation
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Altitude { get; set; }
    public double? Accuracy { get; set; }
    public string? Address { get; set; }
    public string? Description { get; set; }
}

public class TimeRange
{
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }

    public TimeSpan Duration => End - Start;
    public bool Contains(DateTimeOffset timestamp) => timestamp >= Start && timestamp <= End;
}

public class AnalysisResult
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string AnalysisType { get; set; } = string.Empty;
    public DateTimeOffset PerformedAt { get; set; } = DateTimeOffset.UtcNow;
    public string PerformedBy { get; set; } = string.Empty;
    public Dictionary<string, object> Results { get; set; } = new();
    public double Confidence { get; set; }
    public List<string> Tags { get; set; } = new();
}

public enum AttachmentType
{
    Document,
    Image,
    Audio,
    Video,
    Archive,
    Other
}
