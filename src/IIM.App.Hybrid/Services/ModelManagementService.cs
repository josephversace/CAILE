using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace IIM.App.Hybrid.Services;

public class ModelManagementService : IModelManagementService
{
    private readonly ILogger<ModelManagementService> _logger;
    
    public ModelManagementService(ILogger<ModelManagementService> logger)
    {
        _logger = logger;
    }

    public Task<List<Model>> GetModelsAsync()
    {
        throw new NotImplementedException();
    }

    // TODO: Implement service methods
}
