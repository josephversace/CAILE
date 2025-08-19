namespace IIM.Shared.Models;

/// <summary>
/// Represents a geographic location with optional metadata
/// </summary>
public class GeoLocation
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Altitude { get; set; }
    public double? Accuracy { get; set; }
    public string? Address { get; set; }
    public string? Description { get; set; }
}
