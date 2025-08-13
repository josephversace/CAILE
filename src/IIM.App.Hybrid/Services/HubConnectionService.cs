using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace IIM.App.Hybrid.Services;

public class HubConnectionService : IHubConnectionService
{
    private readonly ILogger<HubConnectionService> _logger;
    
    public HubConnectionService(ILogger<HubConnectionService> logger)
    {
        _logger = logger;
    }
    
    // TODO: Implement service methods
}
