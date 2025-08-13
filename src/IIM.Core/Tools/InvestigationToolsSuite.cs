using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace IIM.Core.Tools;

public class InvestigationToolsSuite : IInvestigationToolsSuite
{
    private readonly ILogger<InvestigationToolsSuite> _logger;
    
    public InvestigationToolsSuite(ILogger<InvestigationToolsSuite> logger)
    {
        _logger = logger;
    }
    
    // TODO: Implement service methods
}
