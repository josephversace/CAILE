using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Shared.Enums
{

    public enum ReportType
    {
        Preliminary,
        Progress,
        Final,
        Executive,
        Technical,
        Forensic,
        Intelligence,
        Incident,
        Custom
    }

    public enum ReportStatus
    {
        Draft,
        Review,
        Approved,
        Submitted,
        Archived
    }


    public enum RecommendationPriority
    {
        Low,
        Medium,
        High,
        Urgent
    }

}
