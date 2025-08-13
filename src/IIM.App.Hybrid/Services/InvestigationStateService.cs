using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace IIM.App.Hybrid.Services;

public class InvestigationStateService : IInvestigationStateService
{
    private readonly ILogger<InvestigationStateService> _logger;
    
    public InvestigationStateService(ILogger<InvestigationStateService> logger)
    {
        _logger = logger;
    }
    
    // TODO: Implement service methods
}
