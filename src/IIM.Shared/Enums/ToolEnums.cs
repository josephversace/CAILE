using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Shared.Enums
{
    public enum ToolStatus
    {
        Pending,
        Running,
        Success,
        PartialSuccess,
        Failed,
        Cancelled
    }
}
