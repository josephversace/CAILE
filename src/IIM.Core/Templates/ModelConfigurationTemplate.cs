
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
using IIM.Core.AI;
using IIM.Shared.Interfaces;


namespace IIM.Core.Templates;

//// <summary>
/// Service for managing model configuration templates.
/// This service handles CRUD operations for templates and applies them to investigation sessions.
/// </summary>
public interface IModelConfigurationTemplateService
{
    /// <summary>
    /// Creates a new template from the current session configuration
    /// </summary>
    Task<ModelConfigurationTemplate> CreateTemplateFromSessionAsync(
        string sessionId,
        string templateName,
        string description,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a template to storage
    /// </summary>
    Task<ModelConfigurationTemplate> SaveTemplateAsync(
        ModelConfigurationTemplate template,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a template by ID
    /// </summary>
    Task<ModelConfigurationTemplate?> GetTemplateAsync(
        string templateId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all templates, optionally filtered by category
    /// </summary>
    Task<List<ModelConfigurationTemplate>> GetTemplatesAsync(
        string? category = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing template
    /// </summary>
    Task<ModelConfigurationTemplate> UpdateTemplateAsync(
        ModelConfigurationTemplate template,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a template (user templates only, not system templates)
    /// </summary>
    Task<bool> DeleteTemplateAsync(
        string templateId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clones an existing template with a new name
    /// </summary>
    Task<ModelConfigurationTemplate> CloneTemplateAsync(
        string templateId,
        string newName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a template to an investigation session
    /// </summary>
    Task<InvestigationSession> ApplyTemplateToSessionAsync(
        string templateId,
        string sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads all models specified in a template
    /// </summary>
    Task<bool> LoadModelsFromTemplateAsync(
        string templateId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets system-provided templates
    /// </summary>
    Task<List<ModelConfigurationTemplate>> GetSystemTemplatesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports a template to JSON
    /// </summary>
    Task<string> ExportTemplateAsync(
        string templateId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports a template from JSON
    /// </summary>
    Task<ModelConfigurationTemplate> ImportTemplateAsync(
        string json,
        CancellationToken cancellationToken = default);
}

   