using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace IIM.Core.AI;

public class FineTuningService : IFineTuningService
{
    private readonly ILogger<FineTuningService> _logger;
    
    public FineTuningService(ILogger<FineTuningService> logger)
    {
        _logger = logger;
    }
    
    // TODO: Implement service methods
}
