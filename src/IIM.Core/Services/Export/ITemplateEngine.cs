using System.Threading.Tasks;

namespace IIM.Core.Services;

public interface ITemplateEngine
{
    Task<string> RenderAsync(string template, object model);
    Task<string> RenderTemplateAsync(string templateName, object model);
    Task<string> GetTemplateAsync(string templateName);
    Task SaveTemplateAsync(string templateName, string template);
    Task<bool> TemplateExistsAsync(string templateName);
}
