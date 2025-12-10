using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Dtos;
using Microsoft.AspNetCore.Http;

namespace IIM.Shared.Interfaces;

public interface ILocalModelService
{
	Task<List<LocalModelInfoDto>> ListModelsAsync(string slot, CancellationToken ct = default);

	Task<LocalModelInfoDto> UploadModelAsync(
		string slot,
		string modelName,
		IFormFile zipFile,
		CancellationToken ct = default);
}
