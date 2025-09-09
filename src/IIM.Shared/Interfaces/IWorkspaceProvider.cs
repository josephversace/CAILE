using IIM.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace IIM.Shared.Interfaces;



public interface IWorkspaceProvider
{
    Task<IEnumerable<object>> GetFolderContentsAsync(string path);
    Task<FileReference?> GetFileReferenceAsync(string fileId);
    Task<FileReference> CreateFileReferenceAsync(string path, string fileName, long size, string storageKey);
    Task<FolderReference> CreateFolderAsync(string path, string folderName);
    Task DeleteReferenceAsync(string id);
}