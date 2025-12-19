
using IIM.Core.Services;
using IIM.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Models;

using IIM.Shared.Interfaces;
using IIM.Shared.Models.Core;


namespace IIM.Core.Templates;

//// <summary>
/// Service for managing model configuration templates.
/// This service handles CRUD operations for templates and applies them to investigation sessions.
/// </summary>
public interface IModelConfigurationTemplateService
{
	Task<ModelTemplate> SaveTemplateAsync(ModelTemplate template, CancellationToken cancellationToken = default);
	Task<ModelTemplate?> GetTemplateAsync(string templateId, CancellationToken cancellationToken = default);
	Task<List<ModelTemplate>> GetTemplatesAsync(string? category = null, CancellationToken cancellationToken = default);
	Task<ModelTemplate> UpdateTemplateAsync(ModelTemplate template, CancellationToken cancellationToken = default);
	Task<bool> DeleteTemplateAsync(string templateId, CancellationToken cancellationToken = default);
	Task<ModelTemplate> CloneTemplateAsync(string templateId, string newName, CancellationToken cancellationToken = default);
	Task<List<ModelTemplate>> GetSystemTemplatesAsync(CancellationToken cancellationToken = default);
	Task<string> ExportTemplateAsync(string templateId, CancellationToken cancellationToken = default);
	Task<ModelTemplate> ImportTemplateAsync(string json, CancellationToken cancellationToken = default);
}



