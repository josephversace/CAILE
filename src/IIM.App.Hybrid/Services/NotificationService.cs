using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace IIM.App.Hybrid.Services;

public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;
    
    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger;
    }
    
    // TODO: Implement service methods
}
