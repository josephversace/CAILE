using System.IO.Compression;
using IIM.Shared.Dtos;
using IIM.Shared.Interfaces;
using Microsoft.AspNetCore.Http;

namespace IIM.Infrastructure.Models;

public class LocalModelService : ILocalModelService
{
	public async Task<List<LocalModelInfoDto>> ListModelsAsync(
		string slot,
		CancellationToken ct = default)
	{
		var dir = LocalModelStoragePaths.GetSlotDir(slot);

		if (!Directory.Exists(dir))
			return new List<LocalModelInfoDto>();

		return Directory.GetDirectories(dir)
			.Select(path => new LocalModelInfoDto
			{
			
				Name = Path.GetFileName(path),
			
				SizeBytes = GetDirectorySize(path)
			})
			.ToList();
	}

	public async Task<LocalModelInfoDto> UploadModelAsync(
		string slot,
		string modelName,
		IFormFile zipFile,
		CancellationToken ct = default)
	{
		var targetDir = LocalModelStoragePaths.GetModelDir(slot, modelName);

		if (Directory.Exists(targetDir))
			Directory.Delete(targetDir, true);

		Directory.CreateDirectory(targetDir);

		var tempZipPath = Path.GetTempFileName();

		await using (var fs = File.Create(tempZipPath))
		{
			await zipFile.CopyToAsync(fs, ct);
		}

		ZipFile.ExtractToDirectory(tempZipPath, targetDir, overwriteFiles: true);
		File.Delete(tempZipPath);

		return new LocalModelInfoDto
		{

			Name = modelName
			
		};
	}

	private long GetDirectorySize(string dir)
	{
		return Directory
			.EnumerateFiles(dir, "*", SearchOption.AllDirectories)
			.Sum(f => new FileInfo(f).Length);
	}
}
