using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace IIM.Core.RAG;

public class EmbeddingService : IEmbeddingService
{
    private readonly ILogger<EmbeddingService> _logger;
    
    public EmbeddingService(ILogger<EmbeddingService> logger)
    {
        _logger = logger;
    }
    
    // TODO: Implement service methods
}
