using System;
using IIM.Shared.Mediator;
using Microsoft.AspNetCore.Http;

namespace IIM.Application.Files;

public record UploadFileDirectCommand(
	Guid WorkspaceId,
	IFormFile File
) : IRequest<UploadFileDirectResult>;

public record UploadFileDirectResult(
	Guid FileId,
	string Hash,
	long Size,
	bool WasDeduplicated
);