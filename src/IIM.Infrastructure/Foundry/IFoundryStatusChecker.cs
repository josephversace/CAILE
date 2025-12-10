using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace IIM.Infrastructure.Foundry;

public interface IFoundryStatusChecker
{
	Task<bool> IsServiceRunningAsync();
	Task<bool> StartServiceAsync();
}

public sealed class FoundryStatusChecker : IFoundryStatusChecker
{
	private readonly ILogger<FoundryStatusChecker> _log;

	public FoundryStatusChecker(ILogger<FoundryStatusChecker> log)
	{
		_log = log;
	}

	// ---------------------------------------------------------------------
	// CHECK STATUS
	// ---------------------------------------------------------------------
	public async Task<bool> IsServiceRunningAsync()
	{
		var psi = new ProcessStartInfo
		{
			FileName = "foundry",
			Arguments = "service status",
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true
		};

		using var proc = new Process { StartInfo = psi };

		try
		{
			proc.Start();
		}
		catch (Exception ex)
		{
			_log.LogError(ex, "Failed to run `foundry service status`");
			return false;
		}

		// service status ALWAYS exits quickly and closes stdout
		string output = await proc.StandardOutput.ReadToEndAsync();
		await proc.WaitForExitAsync();

		if (string.IsNullOrWhiteSpace(output))
			return false;

		// Expected output when NOT running:
		// "❌ Model management service is not running!"
		// We simply search for "not running"
		return !output.Contains("not running", StringComparison.OrdinalIgnoreCase);
	}

	// ---------------------------------------------------------------------
	// START SERVICE (non-blocking)
	// ---------------------------------------------------------------------
	public async Task<bool> StartServiceAsync()
	{
		_log.LogInformation("Starting Foundry service via CLI…");

		var psi = new ProcessStartInfo
		{
			FileName = "foundry",
			Arguments = "service start",
			UseShellExecute = false,
			RedirectStandardOutput = false,
			RedirectStandardError = false,
			CreateNoWindow = true
		};

		try
		{
			var proc = Process.Start(psi);
			if (proc == null)
			{
				_log.LogError("Failed to launch Foundry process.");
				return false;
			}
		}
		catch (Exception ex)
		{
			_log.LogError(ex, "Failed to execute `foundry service start`.");
			return false;
		}

		// DO NOT WAIT FOR EXIT — Foundry daemonizes and never returns.
		// A tiny delay helps avoid false negatives when polling immediately.
		await Task.Delay(75);

		_log.LogInformation("Foundry service start command issued (daemonized).");
		return true;
	}
}
