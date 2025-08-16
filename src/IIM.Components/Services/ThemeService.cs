using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace IIM.Components.Services;

public class ThemeService : IThemeService
{
    private readonly ILogger<ThemeService> _logger;
    
    public ThemeService(ILogger<ThemeService> logger)
    {
        _logger = logger;
    }
    
    // TODO: Implement service methods
}
