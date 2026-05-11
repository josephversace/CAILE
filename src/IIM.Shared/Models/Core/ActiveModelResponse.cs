using System;
using System.Collections.Generic;
using System.Text;

namespace IIM.Shared.Models
{
	public class ActiveModelsResponse
	{
		public string? Primary { get; set; }
		public string? Secondary { get; set; }
	}
	public class UploadInitiationResponse
	{
		public string UploadUrl { get; set; }
		public Guid FileId { get; set; }
	}
}
