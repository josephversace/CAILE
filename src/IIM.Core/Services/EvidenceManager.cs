using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace IIM.Core.Services;

public class EvidenceManager : IEvidenceManager
{
    private readonly ILogger<EvidenceManager> _logger;
    
    public EvidenceManager(ILogger<EvidenceManager> logger)
    {
        _logger = logger;
    }
    
    // TODO: Implement service methods
}
