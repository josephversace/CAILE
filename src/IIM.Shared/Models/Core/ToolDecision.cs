using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace IIM.Shared.Models
{
	public sealed record ToolDecision(
	  bool ShouldCallTool,
	  string? ToolName,
	  JsonElement? Arguments,
	  string Confidence
	);

}
