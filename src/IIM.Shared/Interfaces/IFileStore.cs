using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces;

/// <summary>
/// Abstracts physical object storage (SeaweedFS).
/// Collection-first, hash-keyed.
/// </summary>
public interface IFileStore
{
	/// <summary>
	/// Writes a file into a collection using a stable key (e.g. BLAKE3).
	/// </summary>
	Task WriteAsync(
		string collection,
		string key,
		Stream data,
		CancellationToken ct = default);

	/// <summary>
	/// Reads a file by collection + key.
	/// </summary>
	Task<byte[]> ReadAsync(
		string collection,
		string key,
		CancellationToken ct = default);

	/// <summary>
	/// Deletes a file by collection + key.
	/// </summary>
	Task DeleteAsync(
		string collection,
		string key,
		CancellationToken ct = default);

	/// <summary>
	/// Checks if a file exists.
	/// </summary>
	Task<bool> ExistsAsync(
		string collection,
		string key,
		CancellationToken ct = default);

	/// <summary>
	/// Atomically promotes a file between collections.
	/// </summary>
	Task PromoteAsync(
		string sourceCollection,
		string destinationCollection,
		string key,
		CancellationToken ct = default);
}
