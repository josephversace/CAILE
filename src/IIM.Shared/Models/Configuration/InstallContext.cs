using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using IIM.Shared.Models;

namespace IIM.Shared.Models
{

	public class InstallContext
	{

		public string ApiUrl => $"https://localhost:5080";

		// SYSTEM
		public SystemCheckResult SystemCheck { get; set; }

		// FOUNDRY
		public string TierId { get; set; }

		// DB
		public DatabaseConfig Database { get; set; }

		// STORAGE
		public StorageConfig Storage { get; set; }

		public SecretsModel Secrets { get; set; } = new();

		// OUTPUT FILES
		public string DockerComposeYaml { get; set; }
		public string AppSettingsJson { get; set; }


		public IdentitySetupModel Identity { get; set; } = new();

		// PATHS
		public string InstallRoot { get; set; } = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
			".iim");

		public string ApiSettingsPath => Path.Combine(InstallRoot, "appsettings.json");
		public string ComposeFilePath => Path.Combine(InstallRoot, "docker-compose.yml");

		// STATUS
		public bool ReadyForInstall =>
			SystemCheck != null &&
			!string.IsNullOrEmpty(TierId) &&
			Database != null &&
			Storage != null;
	}

}
