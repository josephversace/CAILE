using System;
using System.Collections.Generic;
using System.Text;

namespace IIM.Shared.Models
{

	public class IngestUrlRequest
	{

		public Guid WorkspaceId { get; set; }
		public string Url { get; set; }

		public IngestUrlRequest() { }
		public IngestUrlRequest(Guid workspaceId, string url)
		{
			WorkspaceId = workspaceId;
			Url = url;
		}

		// Optional: parameterless ctor for model binding / serializers
		public IngestUrlRequest(Guid workspaceId) { 
			WorkspaceId = workspaceId;
		}
	}


	public record IngestUrlResult(bool Success, Guid? VirtualFileId, string? Error = null);
}


