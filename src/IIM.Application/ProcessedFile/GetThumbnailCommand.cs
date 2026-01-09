using System;
using System.Collections.Generic;
using System.Text;
using IIM.Shared.Mediator;
using IIM.Shared.Models;

namespace IIM.Application.ProcessedFile
{
    public class GetThumbnailCommand(string storedFileHash, ThumbnailSize size) : IRequest<string>
    {
       public string StoredFiledHash { get; set; } = storedFileHash;

		public ThumbnailSize Size { get; set; } = size;
	}

}
