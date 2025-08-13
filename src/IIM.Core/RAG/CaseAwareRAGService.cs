using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace IIM.Core.RAG;

public class CaseAwareRAGService : ICaseAwareRAGService
{
    private readonly ILogger<CaseAwareRAGService> _logger;
    
    public CaseAwareRAGService(ILogger<CaseAwareRAGService> logger)
    {
        _logger = logger;
    }
    
    // TODO: Implement service methods
}
