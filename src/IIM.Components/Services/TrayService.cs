
using IIM.Core.Platform;

namespace IIM.Components.Services;
public sealed class TrayService
{
    private readonly IWslManager _wsl;
    public TrayService(WslManager wsl) { _wsl = wsl; }
  //  public void EnsureStarted() { if(!_wsl.IsWslEnabled()) _wsl.EnableWsl(); _wsl.StartIim(); }
}
