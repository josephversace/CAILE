using System.Security.Cryptography;
using Blake3;
using IIM.Shared.Interfaces;

namespace IIM.Infrastructure.Services;

public sealed class HashService : IHashService
{
	public async Task<string> ComputeBlake3Async(Stream stream, CancellationToken ct = default)
	{
		stream.Position = 0;
		using var hasher = Hasher.New();

		var buffer = new byte[81920];
		int bytesRead;

		while ((bytesRead = await stream.ReadAsync(buffer, ct)) > 0)
		{
			hasher.Update(buffer.AsSpan(0, bytesRead));
		}

		return hasher.Finalize().ToString();
	}

	public async Task<string> ComputeMd5Async(Stream stream, CancellationToken ct = default)
	{
		stream.Position = 0;
		var hash = await MD5.HashDataAsync(stream, ct);
		return Convert.ToHexString(hash).ToLowerInvariant();
	}

	public async Task<string> ComputeSha256Async(Stream stream, CancellationToken ct = default)
	{
		stream.Position = 0;
		var hash = await SHA256.HashDataAsync(stream, ct);
		return Convert.ToHexString(hash).ToLowerInvariant();
	}

	/// <summary>
	/// Compute BLAKE3, MD5, and SHA-256 in a single pass through the stream.
	/// More efficient than computing each separately.
	/// </summary>
	public async Task<FileHashes> ComputeAllHashesAsync(Stream stream, CancellationToken ct = default)
	{
		stream.Position = 0;

		using var blake3Hasher = Hasher.New();
		using var md5 = IncrementalHash.CreateHash(HashAlgorithmName.MD5);
		using var sha256 = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

		var buffer = new byte[81920];
		int bytesRead;

		while ((bytesRead = await stream.ReadAsync(buffer, ct)) > 0)
		{
			var span = buffer.AsSpan(0, bytesRead);
			blake3Hasher.Update(span);
			md5.AppendData(span);
			sha256.AppendData(span);
		}

		return new FileHashes
		{
			Blake3 = blake3Hasher.Finalize().ToString(),
			Md5 = Convert.ToHexString(md5.GetCurrentHash()).ToLowerInvariant(),
			Sha256 = Convert.ToHexString(sha256.GetCurrentHash()).ToLowerInvariant()
		};
	}
}
