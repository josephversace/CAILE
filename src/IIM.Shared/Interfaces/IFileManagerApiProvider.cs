using IIM.Shared.Models;
using IIM.Shared.Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces
{
    public interface IFileManagerApiProvider<TItem> where TItem : class, new()
    {
        Task<FileManagerEntry<TItem>> GetItemsAsync(string path, CancellationToken ct);
        Task<FileManagerEntry<TItem>> SearchAsync(string query, string path, CancellationToken ct);
        Task<ClassificationMetadata> GetClassificationAsync(string itemId, CancellationToken ct);
        Task UpdateClassificationAsync(string itemId, ClassificationUpdate update, CancellationToken ct);
    }
}
