using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Shared.Enums
{



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



    // Notification Related Enums
    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error,
        Critical,
        Alert,
        System,
        User,
        Case,
        Evidence,
        Investigation,
        Report
    }

    public enum NotificationPriority
    {
        Low,
        Normal,
        High,
        Urgent,
        Critical
    }

    public enum NotificationStatus
    {
        Unread,
        Read,
        Archived,
        Deleted,
        Expired
    }



 

   
}