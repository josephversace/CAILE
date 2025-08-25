using IIM.Shared.Enums;
using IIM.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces
{
    public interface IVisualizationService
    {
        ResponseDisplayType DetermineDisplayType(VisualizationType vizType);
        ResponseDisplayType DetermineDisplayTypeFromToolResult(ToolResult result);
        VisualizationType InferVisualizationType(object data, string? hint = null);
        bool ShouldUseAdvancedDisplay(ToolResult result);
    }
}
