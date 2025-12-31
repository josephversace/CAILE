using System;
using System.Collections.Generic;
using System.Text;

namespace IIM.Shared.Models
{
	public class TextAnalysisMetadata
	{
		public int DocumentLength { get; set; }
		public bool WasTruncated { get; set; }
		public string Preview { get; set; } = "";
	}
}
