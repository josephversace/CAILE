using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IIM.Plugin.SDK.Security;
using IIM.Shared.DTOs;
using Microsoft.Extensions.Logging;

namespace IIM.Core.Plugins.Security.Implementations;

public class RestrictedFileSystem : ISecureFileSystem
{
    private readonly string _basePath;
    private readonly ILogger<RestrictedFileSystem> _logger;

    public RestrictedFileSystem(string basePath, ILogger<RestrictedFileSystem> logger)
    {
        _basePath = basePath;
        _logger = logger;
    }

    public async Task<byte[]> ReadFileAsync(string path, CancellationToken cancellationToken = default)
    {
        var fullPath = GetSafePath(path);
        return await File.ReadAllBytesAsync(fullPath, cancellationToken);
    }

    public async Task<string> ReadTextAsync(string path, CancellationToken cancellationToken = default)
    {
        var fullPath = GetSafePath(path);
        return await File.ReadAllTextAsync(fullPath, cancellationToken);
    }

    public async Task WriteFileAsync(string path, byte[] data, CancellationToken cancellationToken = default)
    {
        var fullPath = GetSafePath(path);
        await File.WriteAllBytesAsync(fullPath, data, cancellationToken);
    }

    public async Task WriteTextAsync(string path, string text, CancellationToken cancellationToken = default)
    {
        var fullPath = GetSafePath(path);
        await File.WriteAllTextAsync(fullPath, text, cancellationToken);
    }

    public Task<bool> FileExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        var fullPath = GetSafePath(path);
        return Task.FromResult(File.Exists(fullPath));
    }

    public Task<FileMetadata?> GetFileMetadataAsync(string path, CancellationToken cancellationToken = default)
    {
        var fullPath = GetSafePath(path);
        if (!File.Exists(fullPath))
            return Task.FromResult<FileMetadata?>(null);

        var info = new FileInfo(fullPath);
        return Task.FromResult<FileMetadata?>(new FileMetadata
        {
            FilePath = path,
            Size = info.Length,
            CreatedAt = info.CreationTime,
            ModifiedAt = info.LastWriteTime,
            Hash = string.Empty,
            MimeType = "application/octet-stream"
        });
    }

    public Task<string[]> ListFilesAsync(string directory, string searchPattern = "*", CancellationToken cancellationToken = default)
    {
        var fullPath = GetSafePath(directory);
        var files = Directory.GetFiles(fullPath, searchPattern);
        return Task.FromResult(files);
    }

    private string GetSafePath(string path)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_basePath, path));
        if (!fullPath.StartsWith(_basePath))
            throw new UnauthorizedAccessException("Access denied to path outside sandbox");
        return fullPath;
    }
}
