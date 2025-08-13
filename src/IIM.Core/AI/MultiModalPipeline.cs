using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace IIM.Core.AI;

public class MultiModalPipeline : IMultiModalPipeline
{
    private readonly ILogger<MultiModalPipeline> _logger;
    
    public MultiModalPipeline(ILogger<MultiModalPipeline> logger)
    {
        _logger = logger;
    }
    
    // TODO: Implement service methods
}
