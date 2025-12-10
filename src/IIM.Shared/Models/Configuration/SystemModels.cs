using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace IIM.Shared.Models
{

	public class NetworkInfo
	{
		public string Name { get; set; } = string.Empty;
		public string IpAddress { get; set; } = string.Empty;
		public string Gateway { get; set; } = string.Empty;
		public string Subnet { get; set; } = string.Empty;
	}



	public class ServiceHealth
	{
		public string ServiceName { get; set; } = string.Empty;
		public bool IsHealthy { get; set; }
		public string Status { get; set; } = string.Empty;
		public DateTime LastChecked { get; set; } = DateTime.UtcNow;
	}



	public class ContainerStatus
	{
		public string Id { get; set; } = string.Empty;
		public string Name { get; set; } = string.Empty;
		public string Status { get; set; } = string.Empty;
		public string Image { get; set; } = string.Empty;
		public DateTime CreatedAt { get; set; }
	}


	public class SystemCheckResult
	{

		public string distro { get; set; } = "Ubuntu 22.04";
		// -------------------------------------------------------
		// INTERNAL STATE (used by UI flow)
		// -------------------------------------------------------
		public bool IsRunning { get; set; }
		public bool HasRun { get; set; }

		// -------------------------------------------------------
		// PACKAGE MANAGER (winget/mac brew)
		// -------------------------------------------------------
		public bool? HasPackageManager { get; set; }
		public string? PackageManagerName { get; set; }

		// -------------------------------------------------------
		// CONTAINER RUNTIME (Docker or Colima)
		// -------------------------------------------------------
		public bool? HasDocker { get; set; }
		public string? DockerVariant { get; set; }  // "Docker Desktop", "Docker Engine (WSL)", "Colima"

		// WINDOWS: WSL + Ubuntu status
		public bool? HasWsl2 { get; set; }
		public string? WslVersion { get; set; }
		public bool? HasDefaultWslDistro { get; set; }
		public bool? UbuntuInstalled { get; set; }
		public bool? DockerEngineInsideWsl { get; set; }

		// MAC: Colima
		public bool? ColimaInstalled { get; set; }

		// -------------------------------------------------------
		// FOUNDRY AI RUNTIME
		// -------------------------------------------------------
		public bool? HasFoundry { get; set; }
		public string? FoundryVersion { get; set; }
		public string? FoundryPath { get; set; }

		public string? FoundryRecommendedTier { get; set; } = "mini";
		// -------------------------------------------------------
		// DISK + MEMORY
		// -------------------------------------------------------
		public double? FreeDiskGb { get; set; }
		public double? TotalRamGb { get; set; }
		public bool? HasEnoughDisk { get; set; }
		public bool? HasEnoughMemory { get; set; }

		// -------------------------------------------------------
		// LOGGING
		// -------------------------------------------------------
		public List<string> Messages { get; set; } = new();

		// -------------------------------------------------------
		// GPU / ACCELERATION CAPABILITIES
		// -------------------------------------------------------
		public bool? HasCuda { get; set; }          // NVIDIA CUDA?
		public bool? HasRocm { get; set; }          // AMD ROCm?
		public double? GpuVramGb { get; set; }      // VRAM total (GB)
		public string? GpuName { get; set; }        // "RTX 4090", "RX 7900 XT", "M2 Max", etc.
		public string? GpuBackend { get; set; }     // "CUDA", "ROCm", "Metal", "None"

		// WSL + Ubuntu extended checks
		public bool? UbuntuInitialized { get; set; }
		public bool? SystemdEnabledInWsl { get; set; }
		public bool? DockerPermissionOk { get; set; }


	}
}