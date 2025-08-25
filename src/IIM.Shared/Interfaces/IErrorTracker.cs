using IIM.Shared.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace IIM.Shared.Interfaces
{
    /// <summary>
    /// Tracks errors for pattern detection and alerting
    /// </summary>
    public interface IErrorTracker
    {
        void TrackError(ErrorEntry error);
        ErrorSummary GetSummary(TimeSpan window);
        List<ErrorPattern> DetectPatterns();
    }

    
}
