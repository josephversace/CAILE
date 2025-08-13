using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace IIM.Core.Services;

public class CaseManager : ICaseManager
{
    private readonly ILogger<CaseManager> _logger;
    
    public CaseManager(ILogger<CaseManager> logger)
    {
        _logger = logger;
    }
    
    // TODO: Implement service methods
}
