using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces;

public interface IHashService
{
    Task<string> ComputeBlake3Async(Stream stream, CancellationToken ct = default);
    Task<string> ComputeMd5Async(Stream stream, CancellationToken ct = default);
    Task<string> ComputeSha256Async(Stream stream, CancellationToken ct = default);

    /// <summary>
    /// Compute all three hashes in a single pass for efficiency.
    /// </summary>
    Task<FileHashes> ComputeAllHashesAsync(Stream stream, CancellationToken ct = default);
}

public sealed class FileHashes
{
    public required string Blake3 { get; init; }
    public required string Md5 { get; init; }
    public required string Sha256 { get; init; }
}
