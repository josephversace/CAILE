using System;
using System.Collections.Generic;
using System.Text;

namespace IIM.Shared.Models.Configuration
{
	public sealed class KreuzbergConfig
	{
		public string BaseUrl { get; init; } = default!;
		public int TimeoutSeconds { get; init; } = 300;
		public int MaxFileSizeMb { get; init; } = 500;
		public bool EnableOcr { get; init; } = true;
		public bool Preferred { get; init; } = true;
	}

}
