using System;
using System.Collections.Generic;
using System.Text;
using IIM.Shared.Models;

namespace IIM.Ingestion.Services
{
	public interface IFastTextExtractor
	{
		Task<ExtractedDocument?> TryExtractAsync(
			byte[] bytes,
			string fileName,
			string mimeType,
			CancellationToken ct);
	}

}
