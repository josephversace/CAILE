using System;
using System.Collections.Generic;
using System.Text;
using IIM.Shared.Models;

namespace IIM.Shared.Dtos
{
	public class AddIndicatorsRequest
	{
		public Guid WorkspaceId { get; set; }
		public Guid? SourceFileId { get; set; }
		public string? SourceFileName { get; set; }
		public Guid? SourceExtractionId { get; set; }
		public List<IndicatorDto> Indicators { get; set; } = new();
	}

	public class IndicatorDto
	{
		public string Value { get; set; } = "";
		public IndicatorType Type { get; set; }
		public string? Subtype { get; set; }
		public float Confidence { get; set; }
	}
}
