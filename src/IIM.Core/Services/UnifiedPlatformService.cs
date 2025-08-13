using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace IIM.Core.Services;

public class UnifiedPlatformService : IUnifiedPlatformService
{
    private readonly ILogger<UnifiedPlatformService> _logger;
    
    public UnifiedPlatformService(ILogger<UnifiedPlatformService> logger)
    {
        _logger = logger;
    }
    
    // TODO: Implement service methods
}
