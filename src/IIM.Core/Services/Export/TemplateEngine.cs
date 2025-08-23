using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RazorLight;

namespace IIM.Core.Services;


using Microsoft.Extensions.Options;

public class TemplateEngineOptions
{
    public string TemplatePath { get; set; }
}


public class TemplateEngine : ITemplateEngine
{
    private readonly ILogger<TemplateEngine> _logger;
    private readonly RazorLightEngine _engine;
    private readonly string _templatePath;

    public TemplateEngine(ILogger<TemplateEngine> logger, IOptions<TemplateEngineOptions> options)
    {
        _logger = logger;
        _templatePath = options.Value.TemplatePath;

        _engine = new RazorLightEngineBuilder()
            .UseFileSystemProject(_templatePath)
            .UseMemoryCachingProvider()
            .Build();
    }

    
    public TemplateEngine(ILogger<TemplateEngine> logger, string templatePath)
    {
        _logger = logger;
        _templatePath = templatePath;
        
        _engine = new RazorLightEngineBuilder()
            .UseFileSystemProject(_templatePath)
            .UseMemoryCachingProvider()
            .Build();
    }
    
    public async Task<string> RenderAsync(string template, object model)
    {
        try
        {
            string result = await _engine.CompileRenderStringAsync(
                Guid.NewGuid().ToString(), 
                template, 
                model);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to render template");
            throw;
        }
    }
    
    public async Task<string> RenderTemplateAsync(string templateName, object model)
    {
        try
        {
            string result = await _engine.CompileRenderAsync(templateName, model);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to render template {TemplateName}", templateName);
            throw;
        }
    }
    
    public async Task<string> GetTemplateAsync(string templateName)
    {
        var templatePath = Path.Combine(_templatePath, $"{templateName}.cshtml");
        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException($"Template {templateName} not found");
        }
        
        return await File.ReadAllTextAsync(templatePath);
    }
    
    public async Task SaveTemplateAsync(string templateName, string template)
    {
        var templatePath = Path.Combine(_templatePath, $"{templateName}.cshtml");
        await File.WriteAllTextAsync(templatePath, template);
        _logger.LogInformation("Saved template {TemplateName}", templateName);
    }
    
    public async Task<bool> TemplateExistsAsync(string templateName)
    {
        var templatePath = Path.Combine(_templatePath, $"{templateName}.cshtml");
        return File.Exists(templatePath);
    }
}
