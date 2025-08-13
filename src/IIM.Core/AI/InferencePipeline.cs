using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace IIM.Core.AI;

public class InferencePipeline : IInferencePipeline
{
    private readonly ILogger<InferencePipeline> _logger;
    
    public InferencePipeline(ILogger<InferencePipeline> logger)
    {
        _logger = logger;
    }
    
    // TODO: Implement service methods
}
