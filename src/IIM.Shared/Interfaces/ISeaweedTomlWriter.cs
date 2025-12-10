using IIM.Shared.Models;

namespace IIM.Shared.Interfaces;

public interface ISeaweedTomlWriter
{
    void WriteAll(InstallContext ctx);
}