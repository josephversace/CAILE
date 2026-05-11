using System;
using System.Collections.Generic;
using System.Text;
using IIM.Shared.Models;

namespace IIM.Shared.Dtos
{
	public sealed class ModelCatalogDto
	{
		public ProviderDescriptorDto Provider { get; set; } = new();
		public IReadOnlyList<ModelCatalogEntryDto> Models { get; set; }
	}

	public sealed class ProviderDescriptorDto
	{
		public string Type { get; set; } = "";
		public string Endpoint { get; set; } = "";
		public bool RequiresApiKey { get; set; }
	}


}
