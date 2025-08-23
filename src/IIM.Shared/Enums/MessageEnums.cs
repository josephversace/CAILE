using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Shared.Enums
{
    /// <summary>
    /// Status of a message in the conversation.
    /// </summary>
    public enum MessageStatus
    {
        Pending,
        Processing,
        Completed,
        Failed,
        Edited,
        Deleted
    }
    public enum MessageRole
    {
        User,
        Assistant,
        System,
        Tool
    }


}
