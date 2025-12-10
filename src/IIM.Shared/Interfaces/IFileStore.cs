using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces;

/// <summary>
/// Abstracts SeaweedFS Filer API for byte-level reads/writes.
/// </summary>
public interface IFileStore
{
	/// <summary>
	/// Write bytes to the specified SeaweedFS logical path.
	/// </summary>
	Task<string> WriteAsync(byte[] data, string path, CancellationToken ct = default);

	/// <summary>
	/// Write a stream to the specified SeaweedFS logical path.
	/// </summary>
	Task<string> WriteAsync(Stream data, string path, CancellationToken ct = default);

	/// <summary>
	/// Read the bytes of a stored file by path.
	/// </summary>
	Task<byte[]> ReadAsync(string path, CancellationToken ct = default);

	/// <summary>
	/// Delete the file at the specified path.
	/// </summary>
	Task DeleteAsync(string path, CancellationToken ct = default);

	/// <summary>
	/// Check if a file exists at the specified path.
	/// </summary>
	Task<bool> ExistsAsync(string path, CancellationToken ct = default);
}