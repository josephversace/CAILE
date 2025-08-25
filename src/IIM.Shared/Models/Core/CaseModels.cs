using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Shared.Models
{
    #region CaseEndpoints
    /// <summary>
    /// Request for batch case updates
    /// </summary>
    public record BatchUpdateCasesRequest(
        List<string> CaseIds,
        string? Status,
        string? Priority,
        string? Classification,
        Dictionary<string, object>? Metadata);


    #endregion

}
