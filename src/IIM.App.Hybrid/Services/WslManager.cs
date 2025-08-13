
using System.Diagnostics;

namespace IIM.App.Hybrid.Services;
public sealed class WslManager
{
    public bool IsWslEnabled()
    {
        var result = RunPwsh("$f=Get-WindowsOptionalFeature -Online -FeatureName Microsoft-Windows-Subsystem-Linux; if($f.State -eq 'Enabled'){exit 0}else{exit 1}");
        return result.ExitCode == 0;
    }

    public int EnableWsl()
    {
        var cmd = "dism /online /enable-feature /featurename:VirtualMachinePlatform /all /norestart; dism /online /enable-feature /featurename:Microsoft-Windows-Subsystem-Linux /all /norestart";
        return RunPwsh(cmd).ExitCode;
    }

    public bool DistroExists(string name)
    {
        var r = Run("wsl", "--list --quiet");
        var lines = r.StdOut.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        return lines.Any(l => string.Equals(l.Trim(), name, StringComparison.OrdinalIgnoreCase));
    }

    public void StartIim()
    {
        // Prototype: daemon runs directly on Windows. Production would start WSL services.
    }

    private static (int ExitCode, string StdOut, string StdErr) Run(string file, string args)
    {
        var p = new Process
        {
            StartInfo = new ProcessStartInfo(file, args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        p.Start();
        var o = p.StandardOutput.ReadToEnd();
        var e = p.StandardError.ReadToEnd();
        p.WaitForExit();
        return (p.ExitCode, o, e);
    }

    private static (int ExitCode, string StdOut, string StdErr) RunPwsh(string script)
        => Run("powershell", "-NoProfile -ExecutionPolicy Bypass -Command "" + script + """);
}
