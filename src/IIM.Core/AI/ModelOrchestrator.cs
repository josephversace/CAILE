using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace IIM.Core.AI;

public class ModelOrchestrator : IModelOrchestrator
{
    private readonly ILogger<ModelOrchestrator> _logger;
    
    public ModelOrchestrator(ILogger<ModelOrchestrator> logger)
    {
        _logger = logger;
    }
    
    // TODO: Implement service methods
}
