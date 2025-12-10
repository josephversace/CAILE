using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Dtos;

public interface IModelConfigurationTemplateService
{
	Task<ModelTemplateDto?> GetDefaultTemplateAsync(CancellationToken ct = default);

	Task SaveDefaultTemplateAsync(ModelTemplateDto template, CancellationToken ct = default);

	Task<List<ModelTemplateDto>> GetSystemTemplatesAsync(CancellationToken ct = default);
}
