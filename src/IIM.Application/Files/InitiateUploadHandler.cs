using System;
using System.Collections.Generic;
using System.Text;
using IIM.Shared.Dtos;
using IIM.Shared.Interfaces;
using IIM.Shared.Mediator;

namespace IIM.Application.Files
{
	public class InitiateUploadHandler
		: IRequestHandler<InitiateUploadQuery, InitiateUploadResult>
	{
		private readonly IFileStore _files;

		public InitiateUploadHandler(IFileStore files)
		{
			_files = files;
		}

		public async Task<InitiateUploadResult> Handle(InitiateUploadQuery query, CancellationToken ct)
		{
			// Ask SeaweedFS for a new file ID + upload URL (no DB activity)
			//var (uploadUrl, fid) = await _files.GenerateUploadUrlAsync(query.FileName, ct);

			throw new NotImplementedException();

			//return new InitiateUploadResult(uploadUrl, fid);
		}
	}



}
