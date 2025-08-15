using System;
namespace IIM.Shared.Models;

public class EvidenceContext
{
    public string CaseId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
}
