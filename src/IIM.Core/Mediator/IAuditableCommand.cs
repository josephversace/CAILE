using System;
using System.Collections.Generic;
using System.Text;

namespace Mediator
{
    /// <summary>
    /// Interface for commands that should be audited
    /// </summary>
    public interface IAuditableCommand
    {
        string? SessionId { get; }
        string? CaseNumber { get; }
    }
}
