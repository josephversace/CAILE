using System;
using System.Collections.Generic;
using System.Text;

namespace IIM.Core.AI
{
    public interface IReasoningOrchestrator
    {
        Task ProcessQueryAsync(string query);
     
    }
}
