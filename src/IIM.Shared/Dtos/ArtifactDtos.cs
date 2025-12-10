using System;
using System.Collections.Generic;
using System.Text;
using IIM.Shared.Models;

namespace IIM.Shared.Dtos
{
	public class CreateArtifactDto
	{
		// Required
		public Guid WorkspaceId { get; set; }
		public ArtifactType Type { get; set; }
		public string Title { get; set; } = "";
		public string Summary { get; set; } = "";
		public string Content { get; set; } = "";
		public List<string> Tags { get; set; } = new();

		// Optional / Future-proofing
		public Guid? ParentId { get; set; }  // allows nested notes
		public string? FileName { get; set; }
		public string? ContentType { get; set; }
		public long? SizeBytes { get; set; }
	}


	public class UpdateArtifactDto
	{
		public ArtifactType Type { get; set; }
		public string Title { get; set; } = "";
		public string Summary { get; set; } = "";
		public string Content { get; set; } = "";
		public List<string> Tags { get; set; } = new();

		// Optional fields for scenarios where files or relationships change
		public Guid? ParentId { get; set; }
		public string? FileName { get; set; }
		public string? ContentType { get; set; }
		public long? SizeBytes { get; set; }
	}



}
