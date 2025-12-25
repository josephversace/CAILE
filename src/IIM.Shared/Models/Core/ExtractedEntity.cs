using System;
using System.Collections.Generic;
using System.Text;

namespace IIM.Shared.Models.Core
{
	public class ExtractedEntity
	{
		public Guid Id { get; set; } = Guid.NewGuid();

		public string StoredFileHash { get; set; } = string.Empty;

		public string EntityType { get; set; } = string.Empty; // person, org, location
		public string Name { get; set; } = string.Empty;

		public Dictionary<string, string> Attributes { get; set; } = new();

		public float Confidence { get; set; }

		public DateTimeOffset ExtractedAt { get; set; } = DateTimeOffset.UtcNow;
	}

}
