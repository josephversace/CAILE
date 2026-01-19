using System;
using System.Collections.Generic;
using System.Text;
using IIM.Shared.Models;

namespace IIM.Shared.Interfaces
{
	public interface IArtifactView
	{
		ArtifactType Type { get; }
		string Title { get; }
	}

}
