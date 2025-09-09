// src/IIM.Application/Services/VirtualFileSystemService.cs
using IIM.Shared.Interfaces; // Use the new interfaces from the Shared project

public class VirtualFileSystemService // No interface needed for this class itself
{
    private readonly IWorkspaceProvider _workspaceProvider;
    private readonly IObjectStorageProvider _storageProvider;

    // Inject the new, clean dependencies
    public VirtualFileSystemService(
        IWorkspaceProvider workspaceProvider,
        IObjectStorageProvider storageProvider)
    {
        _workspaceProvider = workspaceProvider;
        _storageProvider = storageProvider;
    }

    public Task<IEnumerable<object>> GetFolderContentsAsync(string path)
    {
        // Delegate metadata operations to the workspace provider
        return _workspaceProvider.GetFolderContentsAsync(path);
    }

    public async Task<string> GetDownloadUrlAsync(string fileId)
    {
        // 1. Get metadata from the workspace provider
        var fileReference = await _workspaceProvider.GetFileReferenceAsync(fileId);
        if (fileReference is null)
        {
            throw new FileNotFoundException("The requested file does not exist.");
        }

        // 2. Use metadata to get a URL from the storage provider
        return await _storageProvider.GetPresignedDownloadUrlAsync("your-bucket-name", fileReference.StorageKey, TimeSpan.FromMinutes(15));
    }
}