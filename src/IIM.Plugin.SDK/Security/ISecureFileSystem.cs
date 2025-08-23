using System;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.DTOs;

namespace IIM.Plugin.SDK.Security;

/// <summary>
/// Secure file system access for plugins
/// </summary>
public interface ISecureFileSystem
{
    /// <summary>
    /// Read a file as bytes with security checks
    /// </summary>
    Task<byte[]> ReadFileAsync(string path, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Read a file as text with security checks
    /// </summary>
    Task<string> ReadTextAsync(string path, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Write a file with security checks
    /// </summary>
    Task WriteFileAsync(string path, byte[] data, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Write text to a file with security checks
    /// </summary>
    Task WriteTextAsync(string path, string text, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if a file exists
    /// </summary>
    Task<bool> FileExistsAsync(string path, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get file metadata
    /// </summary>
    Task<FileMetadata?> GetFileMetadataAsync(string path, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// List files in a directory
    /// </summary>
    Task<string[]> ListFilesAsync(string directory, string searchPattern = "*", CancellationToken cancellationToken = default);
}
