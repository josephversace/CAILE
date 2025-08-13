using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace IIM.Core.Services;

public class InvestigationService : IInvestigationService
{
    private readonly ILogger<InvestigationService> _logger;
    
    public InvestigationService(ILogger<InvestigationService> logger)
    {
        _logger = logger;
    }
    
    // TODO: Implement service methods
}
