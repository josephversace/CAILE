using IIM.Shared.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Desktop.Services
{


    // Fluxor implementation
    public class FileManagerState
    {
        public string CurrentPath { get; init; } = "/";
        public ImmutableList<FileItem> Items { get; init; } = ImmutableList<FileItem>.Empty;
        public ImmutableHashSet<string> SelectedIds { get; init; } = ImmutableHashSet<string>.Empty;
        public ImmutableDictionary<string, ClassificationData> Classifications { get; init; }
        public FileItem? SelectedFile { get; init; }
        public bool IsLoading { get; init; }
        public string? ErrorMessage { get; init; }
    }
}
