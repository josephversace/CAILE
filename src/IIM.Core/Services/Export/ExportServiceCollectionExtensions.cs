using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IIM.Core.Services;

public static class ExportServiceCollectionExtensions
{
   

    public static IServiceCollection AddExportServices(this IServiceCollection services, string basePath = null)
    {
        // Use app base directory if no path provided
        if (string.IsNullOrEmpty(basePath))
        {
            basePath = AppDomain.CurrentDomain.BaseDirectory;
        }

   

        //Export services
        //Security services
        services.AddScoped<ISecurityService, SecurityService>();

        services.AddScoped<IExportService, ExportService>();

        //File services

        services.AddScoped<IFileService, FileService>();



        return services;
    }

    private static void CreateDefaultTemplates(string templatePath)
    {
        // Create a default PDF template
        var pdfTemplate = @"
@model IIM.Core.Models.InvestigationResponse
<!DOCTYPE html>
<html>
<head>
    <title>Investigation Response - @Model.Id</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 40px; }
        .header { border-bottom: 2px solid #333; padding-bottom: 10px; }
        .content { margin-top: 20px; }
        .footer { margin-top: 40px; text-align: center; font-size: 10pt; color: #666; }
    </style>
</head>
<body>
    <div class='header'>
        <h1>Investigation Response</h1>
        <p>ID: @Model.Id</p>
    </div>
    <div class='content'>
        <p>@Model.Message</p>
    </div>
    <div class='footer'>
        <p>CONFIDENTIAL - LAW ENFORCEMENT ONLY</p>
    </div>
</body>
</html>";

        File.WriteAllText(Path.Combine(templatePath, "ResponsePdf.cshtml"), pdfTemplate);
    }
}
