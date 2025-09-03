using IIM.Shared.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces
{
    namespace IIM.Desktop.Services
    {
        public interface IFileManagerApiClient
        {
            // File operations
            Task<FileListResponse> GetFilesAsync(GetFilesRequest request, CancellationToken ct = default);
            Task<TreeStructureResponse> GetTreeStructureAsync(CancellationToken ct = default);
            Task<FileUploadResponse> UploadFileAsync(string path, Stream fileStream, string fileName, CancellationToken ct = default);
            Task<string> GetDownloadUrlAsync(string fileId);
            Task<string> GetPreviewUrlAsync(string fileId);
            Task<DeleteResponse> DeleteFilesAsync(IEnumerable<string> fileIds);
            Task<FileItem> CreateFolderAsync(string path, string folderName);
            Task<FileItem> RenameAsync(string fileId, string newName);
            Task<MoveResponse> MoveFilesAsync(IEnumerable<string> fileIds, string targetPath);

            // Classification operations
            Task<ClassificationMetadata> GetClassificationAsync(string fileId);
            Task UpdateClassificationAsync(string fileId, ClassificationUpdate update);
            Task<BulkClassificationResponse> BulkClassifyAsync(BulkClassificationRequest request);

            // AI operations
            Task<AIAnalysisResponse> AnalyzeFileAsync(string fileId);
            Task<ChatResponse> ChatAboutFileAsync(string fileId, string message);

            // Search
            Task<SearchResponse> SearchAsync(string query, string path, CancellationToken ct = default);
        }
    }
}
