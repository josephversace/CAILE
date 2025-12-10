using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces
{
	public interface IFilePickerService
	{
		Task<string?> PickFileAsync();
		Task<string?> PickFolderAsync();
	}

}
