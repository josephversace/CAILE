using System;
using System.Collections.Generic;
using System.Text;
using IIM.Ingestion.Models;
using IIM.Shared.Models;

namespace IIM.Ingestion.Services
{
	public interface IDocumentExtractionService
	{
		Task<ExtractedDocument> ExtractAsync(
			byte[] bytes,
			string fileName,
			string mimeType,
			CancellationToken ct);
	}

	

}
