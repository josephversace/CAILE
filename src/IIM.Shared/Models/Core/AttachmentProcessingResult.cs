namespace IIM.Shared.Models;

public class AttachmentProcessingResult
{
	public string? FileId { get; set; }
	public string? ExtractedText { get; set; }
	public bool Success { get; set; }
	public string? Error { get; set; }
	public string? Blake3Hash { get; set; }
}