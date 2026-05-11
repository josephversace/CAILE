using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Models.Core;

namespace IIM.Shared.Interfaces
{
    public interface IPromptSnapshotProvider
    {
        Task<PromptSnapshot> GetSnapshotAsync(bool forceReload = false, CancellationToken ct = default);
    }
}