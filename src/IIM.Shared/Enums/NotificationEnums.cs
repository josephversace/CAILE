using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Shared.Enums
{
    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error,
        Critical
    }


    public enum NotificationCategory
    {
        System,
        Investigation,
        Case,
        Evidence,
        Model,
        Training,
        Export,
        Import,
        Security,
        Update
    }

    public enum NotificationPriority
    {
        Low,
        Normal,
        High,
        Urgent,
        Critical
    }
}