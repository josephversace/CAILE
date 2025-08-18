// IIM.Core/Services/FileService.cs
using Microsoft.Extensions.Logging;

namespace IIM.Core.Services;

public class FileService : IFileService
{
    private readonly ILogger<FileService> _logger;

    public FileService(ILogger<FileService> logger)
    {
        _logger = logger;
    }

    public async Task<byte[]> ReadFileAsync(string path)
    {
        return await File.ReadAllBytesAsync(path);
    }

    public async Task WriteFileAsync(string path, byte[] data)
    {
        await File.WriteAllBytesAsync(path, data);
    }

    public Task<bool> FileExistsAsync(string path)
    {
        return Task.FromResult(File.Exists(path));
    }

    public Task DeleteFileAsync(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        return Task.CompletedTask;
    }

    public Task<string> GetTempFilePathAsync(string extension)
    {
        var fileName = $"{Guid.NewGuid()}.{extension.TrimStart('.')}";
        var path = Path.Combine(Path.GetTempPath(), fileName);
        return Task.FromResult(path);
    }

    public Task<long> GetFileSizeAsync(string path)
    {
        var fileInfo = new FileInfo(path);
        return Task.FromResult(fileInfo.Exists ? fileInfo.Length : 0);
    }
}