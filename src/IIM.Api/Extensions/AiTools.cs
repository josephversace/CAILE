using Microsoft.Extensions.AI;
using System.ComponentModel;

namespace IIM.Api.Extensions;

public static class AiTools
{
	public static IList<AITool> BuildTools(IEnumerable<string> names)
	{
		var list = new List<AITool>();

		foreach (var tool in names)
		{
			switch (tool)
			{
				case "hash.compute":
					list.Add(AIFunctionFactory.Create(ComputeHash));
					break;

				case "docling.parse":
					list.Add(AIFunctionFactory.Create(DoclingParse));
					break;

				case "vfs.read":
					list.Add(AIFunctionFactory.Create(VfsRead));
					break;
			}
		}

		return list;
	}

	// --------------------------------------------
	// Tools Implementations
	// --------------------------------------------

	[Description("Computes SHA256 hash of a string.")]
	public static string ComputeHash(string input) =>
		Convert.ToHexString(
			System.Security.Cryptography.SHA256.HashData(
				System.Text.Encoding.UTF8.GetBytes(input)));

	[Description("Mock Docling parser.")]
	public static string DoclingParse(string fileId) =>
		$"Docling parsed {fileId}.";

	[Description("Mock VFS read.")]
	public static string VfsRead(string path) =>
		$"Read {path} from VFS.";
}
