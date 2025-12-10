using Blake3;
using IIM.Shared.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Infrastructure.Services
{
	public class HashService : IHashService
	{
		private readonly ILogger<HashService> _logger;
		private const int BufferSize = 81920; // 80KB optimal for streams

		public HashService(ILogger<HashService> logger)
		{
			_logger = logger;
		}

		// ------------------------------------------------------------
		// BLAKE3 — Primary hash for CAILE
		// ------------------------------------------------------------
		public async Task<string> ComputeBlake3Async(Stream stream, CancellationToken ct = default)
		{
			if (stream == null) throw new ArgumentNullException(nameof(stream));
			if (!stream.CanRead) throw new IOException("Stream is not readable");

			using var hasher = Hasher.New();
			var buffer = new byte[BufferSize];

			int read;
			while ((read = await stream.ReadAsync(buffer.AsMemory(0, BufferSize), ct)) > 0)
			{
				hasher.Update(buffer.AsSpan(0, read));
			}

			// Reset stream so the caller can reuse it
			if (stream.CanSeek) stream.Position = 0;

			var hash = hasher.Finalize();
			return ToHex(hash.AsSpan());
		}

		public Task<string> ComputeBlake3Async(byte[] data, CancellationToken ct = default)
		{
			using var hasher = Hasher.New();
			hasher.Update(data);
			var hash = hasher.Finalize();
			return Task.FromResult(ToHex(hash.AsSpan()));
		}

		// ------------------------------------------------------------
		// MD5 — Legacy / interop only
		// ------------------------------------------------------------
		public async Task<string> ComputeMd5Async(Stream stream, CancellationToken ct = default)
		{
			if (stream == null) throw new ArgumentNullException(nameof(stream));

			using var md5 = MD5.Create();
			var buffer = new byte[BufferSize];

			int read;
			while ((read = await stream.ReadAsync(buffer.AsMemory(0, BufferSize), ct)) > 0)
			{
				md5.TransformBlock(buffer, 0, read, null, 0);
			}

			md5.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

			if (stream.CanSeek) stream.Position = 0;

			return ToHex(md5.Hash);
		}

		// ------------------------------------------------------------
		// SHA-256 — Standard cryptographic hash for chain-of-custody
		// ------------------------------------------------------------
		public async Task<string> ComputeSha256Async(Stream stream, CancellationToken ct = default)
		{
			if (stream == null) throw new ArgumentNullException(nameof(stream));

			using var sha = SHA256.Create();
			var buffer = new byte[BufferSize];

			int read;
			while ((read = await stream.ReadAsync(buffer.AsMemory(0, BufferSize), ct)) > 0)
			{
				sha.TransformBlock(buffer, 0, read, null, 0);
			}

			sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

			if (stream.CanSeek) stream.Position = 0;

			return ToHex(sha.Hash);
		}

		// ------------------------------------------------------------
		// Utility
		// ------------------------------------------------------------
		private static string ToHex(ReadOnlySpan<byte> bytes)
		{
			var sb = new StringBuilder(bytes.Length * 2);
			foreach (var b in bytes)
				sb.AppendFormat("{0:x2}", b);
			return sb.ToString();
		}

		private static string ToHex(byte[]? bytes)
		{
			if (bytes == null) return string.Empty;
			var sb = new StringBuilder(bytes.Length * 2);
			foreach (var b in bytes)
				sb.AppendFormat("{0:x2}", b);
			return sb.ToString();
		}
	}
}
