using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Shared.Enums
{
    public enum QuickActionDisplayMode
    {
        Grid,      // Card grid for welcome screens
        Chips,     // Horizontal chips above input
        List,      // Vertical list in sidebar
        Compact    // Icon-only buttons
    }

    /// <summary>
    /// Types of actions available on messages
    /// </summary>
    public enum MessageActionType
    {
        Copy,
        Edit,
        Regenerate,
        Delete,
        Share,
        Export,
        Rerun,
        Pin,
        Flag,
        Annotate,
        Details
    }
}
