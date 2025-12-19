namespace IIM.Shared.Models;

public class ProcessedAttachment
{
	public string Name { get; set; } = "";
	public string ContentType { get; set; } = "";
	public long Size { get; set; }
	public string? ExtractedText { get; set; }
	public string? FileId { get; set; }
	public string? Blake3Hash { get; set; }
}