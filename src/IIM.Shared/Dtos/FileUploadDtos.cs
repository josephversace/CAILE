using System;
using System.Collections.Generic;
using System.Text;
using IIM.Shared.Mediator;
using Microsoft.AspNetCore.Http;

namespace IIM.Shared.Dtos
{
	public class CompleteUploadResponseDto
	{
		public Guid VirtualFileId { get; set; }
		public string StoredFileHash { get; set; } = string.Empty;
		public bool IsDuplicate { get; set; }
	}



	public record UploadFileDirectResult(Guid VirtualFileId);


}
