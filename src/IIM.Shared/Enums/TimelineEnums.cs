using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Shared.Enums
{

    public enum EventType
    {
        Communication,
        Transaction,
        Movement,
        Access,
        Modification,
        Creation,
        Deletion,
        Meeting,
        Observation,
        Other
    }

    public enum EventImportance
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum PatternType
    {
        Temporal,
        Behavioral,
        Transactional,
        Communication,
        Geographic
    }

    public enum AnomalyType
    {
        TimeGap,
        UnusualActivity,
        PatternBreak,
        Outlier,
        Suspicious
    }

    public enum CriticalityLevel
    {
        Low,
        Medium,
        High,
        Critical
    }
}
