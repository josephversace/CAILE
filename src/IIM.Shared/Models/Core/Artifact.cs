using System;
using System.Collections.Generic;
using System.Text;

namespace IIM.Shared.Models
{
	
	public enum ArtifactType { Note, Code, Plan, Research, File }
	public enum FileClass { All, Evidence, Intelligence, Reference, Output }

	public class CanvasArtifact
	{
		public string Id { get; set; } = Guid.NewGuid().ToString();
		public ArtifactType Type { get; set; } = ArtifactType.Note;

		// Common
		public string Title { get; set; } = "";
		public string Summary { get; set; } = "";
		public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
		public List<string> Tags { get; set; } = new();

		// File-specific
		public FileClass? Classification { get; set; } // Only for Type=File
		public string? FileName { get; set; }
		public string? ContentType { get; set; }
		public long? SizeBytes { get; set; }
		public string? Sha256 { get; set; }
		public string? Md5 { get; set; }
		public string? RelatedCaseId { get; set; }

		public string Content { get; set; } = "";

	}

}
