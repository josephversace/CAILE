using System;
using System.Collections.Generic;
using System.Text;
using IIM.Shared.Dtos;
using IIM.Shared.Models;

namespace IIM.Ingestion.Extensions
{
	public static class ProposedEventsMapper
	{
		public static ProposedEventDto MapToDto(ProposedEvent ev)
		{
			return new ProposedEventDto
			{
				Id = ev.Id,
				EventType = ev.EventType,
				Timestamp = ev.Timestamp.Value,

				// FORENSIC FIX: Store coordinates, not the string content
				ContextStart = ev.Timestamp.Context?.BlockStart ?? 0,
				ContextLength = ev.Timestamp.Context?.BlockLength ?? 0,

				Who = ev.Who.Select(i => new IndicatorSummary(i.Type.ToString(), i.Value)).ToList(),
				What = ev.What.Select(i => new IndicatorSummary(i.Type.ToString(), i.Value)).ToList(),
				Where = ev.Where.Select(i => new IndicatorSummary(i.Type.ToString(), i.Value)).ToList()
			};
		}

		public static List<ProposedEventDto> MapToDto(List<ProposedEvent> events)
		{
			List<ProposedEventDto> dtoList = new List<ProposedEventDto>();

			foreach (ProposedEvent ev in events)
			{
				dtoList.Add(MapToDto(ev));
			}

			return dtoList;
		}
	}
}
