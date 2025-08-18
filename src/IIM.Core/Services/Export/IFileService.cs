using System.Threading.Tasks;

namespace IIM.Core.Services;

public interface IFileService
{
    Task<byte[]> ReadFileAsync(string path);
    Task WriteFileAsync(string path, byte[] data);
    Task<bool> FileExistsAsync(string path);
    Task DeleteFileAsync(string path);
    Task<string> GetTempFilePathAsync(string extension);
    Task<long> GetFileSizeAsync(string path);
}
