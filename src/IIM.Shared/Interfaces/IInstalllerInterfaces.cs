using System;
using System.Threading.Tasks;
using IIM.Shared.Models;

namespace IIM.Shared.Interfaces
{
	public interface IDockerComposeBuilder
	{
		string Build(InstallContext ctx);
	}

	public interface IAppSettingsBuilder
	{
		string Build(InstallContext ctx);
	}


	public interface IInstallerService
	{
		/// <summary>
		/// Streams log output to the installer UI (real-time).
		/// </summary>
		event Action<string>? OnLog;

		/// <summary>
		/// Reports progress (0-100) to the installer UI.
		/// </summary>
		event Action<int>? OnProgress;

		/// <summary>
		/// Runs the installer using the fully-prepared InstallContext
		/// from the setup wizard.
		/// </summary>
		/// <param name="ctx">Fully populated install model</param>
		Task RunInstallerAsync(InstallContext ctx);
	}

}
