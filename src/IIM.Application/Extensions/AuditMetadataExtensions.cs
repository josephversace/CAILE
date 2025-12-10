using System;
using System.Collections.Generic;
using System.Text;
using IIM.Shared.Models;

namespace IIM.Application.Extensions
{
	public static class AuditMetadataExtensions
	{
		public static void Set(this List<AuditMetadataItem> list, string key, object value)
		{
			var item = list.FirstOrDefault(x => x.Key == key);
			if (item == null)
				list.Add(new AuditMetadataItem { Key = key, Value = value?.ToString() ?? "" });
			else
				item.Value = value?.ToString() ?? "";
		}
	}

}
