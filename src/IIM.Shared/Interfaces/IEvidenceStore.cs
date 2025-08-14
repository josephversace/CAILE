using System;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces
{
    public interface IEvidenceStore
    {
        Task<string> StoreEvidenceAsync(byte[] data, string metadata);
        Task<byte[]> RetrieveEvidenceAsync(string evidenceId);
        Task<bool> VerifyIntegrityAsync(string evidenceId);
    }
}
