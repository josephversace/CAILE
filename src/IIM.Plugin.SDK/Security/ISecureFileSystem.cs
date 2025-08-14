using System;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Models;

namespace IIM.Plugin.SDK;

/// <summary>
/// Secure file system access for plugins
/// </summary>
public interface ISecureFileSystem
{
    /// <summary>
    /// Read a file with permission and size checks
    /// </summary>
    Task<string> ReadTextAsync(string path, CancellationToken ct = default);
    
    /// <summary>
    /// Read binary file with permission and size checks
    /// </summary>
    Task<byte[]> ReadBytesAsync(string path, CancellationToken ct = default);
    
    /// <summary>
    /// Write text to a file in the plugins temp directory
    /// </summary>
    Task WriteTextAsync(string filename, string content, CancellationToken ct = default);
    
    /// <summary>
    /// Write bytes to a file in the plugins temp directory
    /// </summary>
    Task WriteBytesAsync(string filename, byte[] content, CancellationToken ct = default);
    
    /// <summary>
    /// Check if a file exists and is accessible
    /// </summary>
    Task<bool> ExistsAsync(string path);
    
    /// <summary>
    /// Get file metadata
    /// </summary>
    Task<FileMetadata> GetMetadataAsync(string path);
}

