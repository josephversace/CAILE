using IIM.Shared.Interfaces;
using OpenAI.ObjectModels.ResponseModels.FileResponseModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Desktop.Services
{
    public class ApiFileManagerProvider : IFileManagerApiProvider<ClassifiableFile>
    {
        private readonly HttpClient _httpClient;
        private readonly IStateManagementService _state;

        public async Task<FileManagerEntry<ClassifiableFile>> GetItemsAsync(string path, CancellationToken ct)
        {
            // Check state cache first
            if (_state.TryGetCachedItems(path, out var cached))
                return cached;

            var response = await _httpClient.GetAsync($"/api/files?path={path}", ct);
            var items = await response.Content.ReadFromJsonAsync<FileListResponse>();

            // Update state
            _state.UpdateFileList(path, items);

            return TransformToFileManagerEntry(items);
        }
    }
}
