using System;
using System.Collections.Generic;
using System.Text;

namespace IIM.Shared.Dtos
{
	public class AddEventRequest
	{
		public Guid WorkspaceId { get; set; }
		public Guid? SourceFileId { get; set; }
		public string? SourceFileName { get; set; }
		public EventDto Event { get; set; } = null!;
	}

	public class EventDto
	{
		public string EventType { get; set; } = "Event";
		public string Timestamp { get; set; } = null!;
		public float Confidence { get; set; }
		public List<IndicatorDto> Who { get; set; } = new();
		public List<IndicatorDto> What { get; set; } = new();
		public List<IndicatorDto> Where { get; set; } = new();
	}
}
