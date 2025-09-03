using IIM.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces
{
    public interface IFileManagerStateService
    {
        // Current state
        FileManagerEntry<ClassifiableFile> CurrentDirectory { get; }
        IEnumerable<FileManagerEntry<ClassifiableFile>> SelectedItems { get; }
        ClassifiableFile SelectedFile { get; }

        // Classification state
        Dictionary<string, ClassificationMetadata> Classifications { get; }
        Queue<BulkClassificationItem> ClassificationQueue { get; }

        // Events
        event EventHandler<FileSelectionChangedEventArgs> SelectionChanged;
        event EventHandler<DirectoryChangedEventArgs> DirectoryChanged;
        event EventHandler<ClassificationUpdatedEventArgs> ClassificationUpdated;

        // Methods
        void UpdateCurrentDirectory(FileManagerEntry<ClassifiableFile> directory);
        void UpdateSelection(IEnumerable<FileManagerEntry<ClassifiableFile>> items);
        void UpdateClassification(string fileId, ClassificationMetadata classification);
        void EnqueueBulkClassification(IEnumerable<string> fileIds);

        // Caching
        bool TryGetCachedDirectory(string path, out FileManagerEntry<ClassifiableFile> entry);
        void CacheDirectory(string path, FileManagerEntry<ClassifiableFile> entry, TimeSpan ttl);
    }
}
