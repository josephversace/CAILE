
namespace IIM.Shared.Models;
public sealed class GpuInfo
{
    public string Vendor { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public int VramGb { get; set; }
}
