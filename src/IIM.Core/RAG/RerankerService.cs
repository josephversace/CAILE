using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace IIM.Core.RAG;

public class RerankerService : IRerankerService
{
    private readonly ILogger<RerankerService> _logger;
    
    public RerankerService(ILogger<RerankerService> logger)
    {
        _logger = logger;
    }
    
    // TODO: Implement service methods
}
