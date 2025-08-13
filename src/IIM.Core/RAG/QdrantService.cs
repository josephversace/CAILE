using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace IIM.Core.RAG;

public class QdrantService : IQdrantService
{
    private readonly ILogger<QdrantService> _logger;
    
    public QdrantService(ILogger<QdrantService> logger)
    {
        _logger = logger;
    }
    
    // TODO: Implement service methods
}
