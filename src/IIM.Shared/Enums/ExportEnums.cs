using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Shared.Enums
{
    public enum ExportFormat
    {
        Pdf,
        Word,
        Excel,
        Json,
        Csv,
        Html,
        Markdown,
        Xml,
        Text
    }

    public enum ExportStatus
    {
        Pending,
        Processing,
        Completed,
        Failed,
        Cancelled
    }
}
