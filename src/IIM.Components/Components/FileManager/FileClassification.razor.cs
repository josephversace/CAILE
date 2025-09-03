using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Collections.Immutable;
using IIM.Shared.Models;

    using Fluxor;


namespace IIM.Components.Components.FileManager
{


    public partial class FileClassification : ComponentBase, IDisposable
    {
        #region Injected Services

        [Inject] private IFileManagerApiClient ApiClient { get; set; }
        [Inject] private IDispatcher Dispatcher { get; set; }
        [Inject] private IState<FileManagerState> FileState { get; set; }
        [Inject] private IAIClassificationService AIService { get; set; }
        [Inject] private IJSRuntime JSRuntime { get; set; }
        [Inject] private IDialogService DialogService { get; set; }
        [Inject] private ILogger<DataClassificationExplorer> Logger { get; set; }

        #endregion

        #region Parameters

        [Parameter] public string InitialPath { get; set; } = "/";
        [Parameter] public bool ShowTreePanel { get; set; } = true;
        [Parameter] public bool ShowAIPanel { get; set; } = true;
        [Parameter] public bool EnableBulkOperations { get; set; } = true;
        [Parameter] public bool EnableAIClassification { get; set; } = true;
        [Parameter] public ViewMode DefaultViewMode { get; set; } = ViewMode.Grid;
        [Parameter] public EventCallback<FileItem> OnFileSelected { get; set; }
        [Parameter] public EventCallback<IEnumerable<FileItem>> OnSelectionChanged { get; set; }
        [Parameter] public EventCallback<ClassificationMetadata> OnClassificationUpdated { get; set; }
        [Parameter] public int MaxFileUploadSize { get; set; } = 100 * 1024 * 1024; // 100MB
        [Parameter] public string[] AllowedFileExtensions { get; set; } = new[] { "*" };

        #endregion

        #region State Properties

        // View State
        private ViewMode ViewMode { get; set; }
        private FileSortOrder SortOrder { get; set; } = FileSortOrder.NameAsc;
        private string CurrentPath { get; set; } = "/";
        private string SearchQuery { get; set; } = "";
        private bool IsLoading { get; set; }
        private bool IsDarkMode { get; set; }
        private bool IsTreeCollapsed { get; set; }
        private bool IsAIPanelCollapsed { get; set; }

        // Selection State
        private HashSet<string> SelectedIds { get; set; } = new();
        private List<FileItem> SelectedItems { get; set; } = new();
        private FileItem CurrentFile { get; set; }

        // Data State
        private List<FileItem> DisplayItems { get; set; } = new();
        private List<TreeNode> TreeItems { get; set; } = new();
        private List<BreadcrumbItem> BreadcrumbItems { get; set; } = new();
        private Dictionary<string, ClassificationMetadata> Classifications { get; set; } = new();

        // Statistics
        private int TotalItems { get; set; }
        private int ClassifiedCount { get; set; }
        private int PendingCount { get; set; }
        private string LastOperation { get; set; }

        // UI References
        private FluentDialog uploadDialog;
        private FluentDialog classificationDialog;

        // Navigation History
        private Stack<string> NavigationHistory { get; set; } = new();
        private Stack<string> ForwardHistory { get; set; } = new();

        // Timers and Debouncing
        private Timer _searchDebounceTimer;
        private CancellationTokenSource _loadCancellationTokenSource;

        #endregion

        #region Lifecycle Methods

        protected override async Task OnInitializedAsync()
        {
            ViewMode = DefaultViewMode;
            CurrentPath = InitialPath;

            // Subscribe to state changes
            FileState.StateChanged += OnStateChanged;

            // Load initial data
            await LoadDirectoryAsync(CurrentPath);

            // Initialize tree view
            await LoadTreeStructureAsync();

            // Load theme preference
            IsDarkMode = await JSRuntime.InvokeAsync<bool>("localStorage.getItem", "darkMode");

            // Set up keyboard shortcuts
            await SetupKeyboardShortcuts();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                // Initialize resizable panels
                await JSRuntime.InvokeVoidAsync("initializeResizablePanels");
            }
        }

        public void Dispose()
        {
            FileState.StateChanged -= OnStateChanged;
            _searchDebounceTimer?.Dispose();
            _loadCancellationTokenSource?.Cancel();
            _loadCancellationTokenSource?.Dispose();
        }

        #endregion

        #region Navigation Methods

        private async Task NavigateToPath(string path)
        {
            if (path == CurrentPath) return;

            // Add current path to history
            NavigationHistory.Push(CurrentPath);
            ForwardHistory.Clear();

            CurrentPath = path;
            await LoadDirectoryAsync(path);
            UpdateBreadcrumb();
        }

        private async Task NavigateBack()
        {
            if (NavigationHistory.Count == 0) return;

            ForwardHistory.Push(CurrentPath);
            CurrentPath = NavigationHistory.Pop();
            await LoadDirectoryAsync(CurrentPath);
            UpdateBreadcrumb();
        }

        private async Task NavigateForward()
        {
            if (ForwardHistory.Count == 0) return;

            NavigationHistory.Push(CurrentPath);
            CurrentPath = ForwardHistory.Pop();
            await LoadDirectoryAsync(CurrentPath);
            UpdateBreadcrumb();
        }

        private async Task NavigateUp()
        {
            var parentPath = GetParentPath(CurrentPath);
            if (parentPath != CurrentPath)
            {
                await NavigateToPath(parentPath);
            }
        }

        private async Task NavigateToBreadcrumb(int index)
        {
            var path = string.Join("/", BreadcrumbItems.Take(index + 1).Select(b => b.Name));
            if (string.IsNullOrEmpty(path)) path = "/";
            await NavigateToPath(path);
        }

        private void UpdateBreadcrumb()
        {
            BreadcrumbItems.Clear();

            if (CurrentPath == "/")
            {
                BreadcrumbItems.Add(new BreadcrumbItem { Name = "Home", Path = "/" });
                return;
            }

            var parts = CurrentPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var currentPath = "";

            BreadcrumbItems.Add(new BreadcrumbItem { Name = "Home", Path = "/" });

            foreach (var part in parts)
            {
                currentPath += "/" + part;
                BreadcrumbItems.Add(new BreadcrumbItem { Name = part, Path = currentPath });
            }
        }

        #endregion

        #region Data Loading Methods

        private async Task LoadDirectoryAsync(string path)
        {
            try
            {
                IsLoading = true;
                LastOperation = "Loading...";
                StateHasChanged();

                // Cancel any existing load operation
                _loadCancellationTokenSource?.Cancel();
                _loadCancellationTokenSource = new CancellationTokenSource();

                var request = new GetFilesRequest
                {
                    Path = path,
                    SortOrder = SortOrder,
                    PageSize = 1000,
                    Filters = GetCurrentFilters()
                };

                var response = await ApiClient.GetFilesAsync(request, _loadCancellationTokenSource.Token);

                // Update display items
                DisplayItems = CombineAndSort(response.Files, response.Folders);

                // Load classifications for visible items
                await LoadClassificationsAsync(DisplayItems.Select(i => i.Id));

                // Update statistics
                UpdateStatistics(response.Statistics);

                TotalItems = DisplayItems.Count;
                LastOperation = $"Loaded {TotalItems} items";
            }
            catch (OperationCanceledException)
            {
                // Operation was cancelled, ignore
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to load directory: {Path}", path);
                LastOperation = "Failed to load directory";
                await ShowErrorDialog("Failed to load directory", ex.Message);
            }
            finally
            {
                IsLoading = false;
                StateHasChanged();
            }
        }

        private async Task LoadTreeStructureAsync()
        {
            try
            {
                var treeData = await ApiClient.GetTreeStructureAsync();
                TreeItems = BuildTreeNodes(treeData);
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to load tree structure");
            }
        }

        private async Task LoadClassificationsAsync(IEnumerable<string> fileIds)
        {
            var tasks = fileIds.Select(async id =>
            {
                try
                {
                    var classification = await ApiClient.GetClassificationAsync(id);
                    Classifications[id] = classification;
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Failed to load classification for file: {FileId}", id);
                }
            });

            await Task.WhenAll(tasks);
        }

        private async Task RefreshCurrentView()
        {
            await LoadDirectoryAsync(CurrentPath);
        }

        #endregion

        #region Selection Methods

        private void OnFileClick(FileItem item, MouseEventArgs e)
        {
            if (e.CtrlKey)
            {
                // Toggle selection
                if (SelectedIds.Contains(item.Id))
                {
                    SelectedIds.Remove(item.Id);
                    SelectedItems.Remove(item);
                }
                else
                {
                    SelectedIds.Add(item.Id);
                    SelectedItems.Add(item);
                }
            }
            else if (e.ShiftKey && SelectedItems.Count > 0)
            {
                // Range selection
                var lastSelected = SelectedItems.Last();
                var startIndex = DisplayItems.IndexOf(lastSelected);
                var endIndex = DisplayItems.IndexOf(item);

                if (startIndex >= 0 && endIndex >= 0)
                {
                    var start = Math.Min(startIndex, endIndex);
                    var end = Math.Max(startIndex, endIndex);

                    for (int i = start; i <= end; i++)
                    {
                        var fileItem = DisplayItems[i];
                        if (!SelectedIds.Contains(fileItem.Id))
                        {
                            SelectedIds.Add(fileItem.Id);
                            SelectedItems.Add(fileItem);
                        }
                    }
                }
            }
            else
            {
                // Single selection
                SelectedIds.Clear();
                SelectedItems.Clear();
                SelectedIds.Add(item.Id);
                SelectedItems.Add(item);
                CurrentFile = item;
            }

            StateHasChanged();
            OnSelectionChanged.InvokeAsync(SelectedItems);
        }

        private async Task OnFileDoubleClick(FileItem item)
        {
            if (item.Type == FileItemType.Folder)
            {
                await NavigateToPath(item.VirtualPath);
            }
            else
            {
                await OpenFilePreview(item);
            }
        }

        private void OnSelectionChanged(IEnumerable<FileItem> selectedItems)
        {
            SelectedItems = selectedItems.ToList();
            SelectedIds = selectedItems.Select(i => i.Id).ToHashSet();
            CurrentFile = SelectedItems.FirstOrDefault();
            StateHasChanged();
        }

        private bool IsSelected(FileItem item) => SelectedIds.Contains(item.Id);

        private bool HasSelection => SelectedItems.Count > 0;

        private void ClearSelection()
        {
            SelectedIds.Clear();
            SelectedItems.Clear();
            CurrentFile = null;
            StateHasChanged();
        }

        private void SelectAll()
        {
            SelectedItems = DisplayItems.Where(i => i.Type == FileItemType.File).ToList();
            SelectedIds = SelectedItems.Select(i => i.Id).ToHashSet();
            StateHasChanged();
        }

        #endregion

        #region Classification Methods

        private async Task QuickClassify()
        {
            if (!HasSelection) return;

            await classificationDialog.ShowAsync();
        }

        private async Task BulkClassify()
        {
            if (!HasSelection) return;

            try
            {
                IsLoading = true;
                LastOperation = "Classifying files with AI...";
                StateHasChanged();

                var request = new BulkClassificationRequest
                {
                    FileIds = SelectedIds.ToList(),
                    UseAI = true
                };

                var response = await ApiClient.BulkClassifyAsync(request);

                // Update local classifications
                foreach (var result in response.Results)
                {
                    Classifications[result.FileId] = result.Classification;
                }

                LastOperation = $"Classified {response.SuccessCount} files";
                await RefreshCurrentView();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Bulk classification failed");
                await ShowErrorDialog("Classification Failed", ex.Message);
            }
            finally
            {
                IsLoading = false;
                StateHasChanged();
            }
        }

        private async Task OnClassificationChanged(ClassificationMetadata classification)
        {
            try
            {
                Classifications[classification.FileId] = classification;

                var update = new ClassificationUpdate
                {
                    FileId = classification.FileId,
                    Level = classification.Level,
                    Tags = classification.Tags,
                    Description = classification.Description
                };

                await ApiClient.UpdateClassificationAsync(classification.FileId, update);

                LastOperation = "Classification updated";
                await OnClassificationUpdated.InvokeAsync(classification);
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to update classification");
                await ShowErrorDialog("Update Failed", ex.Message);
            }
        }

        private async Task OnTagsChanged(string fileId, List<string> tags)
        {
            if (Classifications.TryGetValue(fileId, out var classification))
            {
                classification.Tags = tags;
                await OnClassificationChanged(classification);
            }
        }

        private async Task OnClassificationComplete(ClassificationResult result)
        {
            await classificationDialog.HideAsync();
            await RefreshCurrentView();
        }

        #endregion

        #region File Operations

        private async Task CreateFolder()
        {
            var dialog = await DialogService.ShowInputAsync("New Folder", "Enter folder name:");
            if (!string.IsNullOrEmpty(dialog.Value))
            {
                try
                {
                    await ApiClient.CreateFolderAsync(CurrentPath, dialog.Value);
                    await RefreshCurrentView();
                    LastOperation = $"Created folder: {dialog.Value}";
                }
                catch (Exception ex)
                {
                    await ShowErrorDialog("Failed to create folder", ex.Message);
                }
            }
        }

        private async Task ShowUploadDialog()
        {
            await uploadDialog.ShowAsync();
        }

        private async Task OnUploadComplete(FileUploadResponse response)
        {
            await uploadDialog.HideAsync();
            await RefreshCurrentView();
            LastOperation = $"Uploaded {response.UploadedFiles.Count} files";
        }

        private async Task DeleteSelected()
        {
            if (!HasSelection) return;

            var result = await DialogService.ShowConfirmAsync(
                "Delete Files",
                $"Are you sure you want to delete {SelectedItems.Count} item(s)?",
                "Delete",
                "Cancel");

            if (result.Value)
            {
                try
                {
                    await ApiClient.DeleteFilesAsync(SelectedIds);
                    ClearSelection();
                    await RefreshCurrentView();
                    LastOperation = "Files deleted";
                }
                catch (Exception ex)
                {
                    await ShowErrorDialog("Delete Failed", ex.Message);
                }
            }
        }

        private async Task DownloadSelected()
        {
            foreach (var item in SelectedItems.Where(i => i.Type == FileItemType.File))
            {
                try
                {
                    var url = await ApiClient.GetDownloadUrlAsync(item.Id);
                    await JSRuntime.InvokeVoidAsync("window.open", url, "_blank");
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to download file: {FileId}", item.Id);
                }
            }
        }

        private async Task OpenFilePreview(FileItem file)
        {
            try
            {
                var previewUrl = await ApiClient.GetPreviewUrlAsync(file.Id);
                await JSRuntime.InvokeVoidAsync("window.open", previewUrl, "_blank");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to open preview for file: {FileId}", file.Id);
            }
        }

        #endregion

        #region Search Methods

        private void OnSearchInput(ChangeEventArgs e)
        {
            SearchQuery = e.Value?.ToString() ?? "";

            // Debounce search
            _searchDebounceTimer?.Dispose();
            _searchDebounceTimer = new Timer(async _ =>
            {
                await InvokeAsync(async () =>
                {
                    await PerformSearch();
                });
            }, null, 300, Timeout.Infinite);
        }

        private async Task PerformSearch()
        {
            if (string.IsNullOrWhiteSpace(SearchQuery))
            {
                await LoadDirectoryAsync(CurrentPath);
                return;
            }

            try
            {
                IsLoading = true;
                LastOperation = "Searching...";
                StateHasChanged();

                var response = await ApiClient.SearchAsync(SearchQuery, CurrentPath);
                DisplayItems = response.Results.ToList();
                TotalItems = DisplayItems.Count;
                LastOperation = $"Found {TotalItems} items";
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Search failed");
                LastOperation = "Search failed";
            }
            finally
            {
                IsLoading = false;
                StateHasChanged();
            }
        }

        #endregion

        #region UI Methods

        private void SetViewMode(ViewMode mode)
        {
            ViewMode = mode;
            StateHasChanged();
        }

        private async Task ToggleTheme()
        {
            IsDarkMode = !IsDarkMode;
            await JSRuntime.InvokeVoidAsync("localStorage.setItem", "darkMode", IsDarkMode);
            StateHasChanged();
        }

        private void ToggleTreePanel()
        {
            IsTreeCollapsed = !IsTreeCollapsed;
            StateHasChanged();
        }

        private void ToggleAIPanel()
        {
            IsAIPanelCollapsed = !IsAIPanelCollapsed;
            StateHasChanged();
        }

        private async Task OnTreeItemSelected(TreeNode node)
        {
            await NavigateToPath(node.Path);
        }

        private async Task OnBulkClassify(BulkClassificationRequest request)
        {
            try
            {
                var response = await ApiClient.BulkClassifyAsync(request);
                await RefreshCurrentView();
                LastOperation = $"Classified {response.SuccessCount} files";
            }
            catch (Exception ex)
            {
                await ShowErrorDialog("Bulk Classification Failed", ex.Message);
            }
        }

        #endregion

        #region Helper Methods

        private string GetParentPath(string path)
        {
            if (string.IsNullOrEmpty(path) || path == "/") return "/";

            var lastSlash = path.LastIndexOf('/');
            return lastSlash <= 0 ? "/" : path.Substring(0, lastSlash);
        }

        private List<FileItem> CombineAndSort(List<FileItem> files, List<FileItem> folders)
        {
            var combined = new List<FileItem>();
            combined.AddRange(folders);
            combined.AddRange(files);

            return SortOrder switch
            {
                FileSortOrder.NameAsc => combined.OrderBy(i => i.Type).ThenBy(i => i.Name).ToList(),
                FileSortOrder.NameDesc => combined.OrderBy(i => i.Type).ThenByDescending(i => i.Name).ToList(),
                FileSortOrder.DateAsc => combined.OrderBy(i => i.Type).ThenBy(i => i.ModifiedDate).ToList(),
                FileSortOrder.DateDesc => combined.OrderBy(i => i.Type).ThenByDescending(i => i.ModifiedDate).ToList(),
                FileSortOrder.SizeAsc => combined.OrderBy(i => i.Type).ThenBy(i => i.Size).ToList(),
                FileSortOrder.SizeDesc => combined.OrderBy(i => i.Type).ThenByDescending(i => i.Size).ToList(),
                FileSortOrder.ClassificationAsc => combined.OrderBy(i => i.Type)
                    .ThenBy(i => Classifications.ContainsKey(i.Id) ? Classifications[i.Id].Level : DataClassificationLevel.Unclassified)
                    .ToList(),
                _ => combined
            };
        }

        private FileFilterOptions GetCurrentFilters()
        {
            // Build filters based on current UI state
            return new FileFilterOptions
            {
                // Add any active filters here
            };
        }

        private void UpdateStatistics(FileStatistics stats)
        {
            if (stats != null)
            {
                ClassifiedCount = stats.ClassifiedFiles;
                PendingCount = stats.PendingReview;
            }
        }

        private List<TreeNode> BuildTreeNodes(TreeStructureResponse data)
        {
            // Convert API response to tree nodes
            return data.Nodes.Select(n => new TreeNode
            {
                Name = n.Name,
                Path = n.Path,
                IsExpanded = n.IsExpanded,
                HasChildren = n.HasChildren,
                Children = n.Children != null ? BuildTreeNodesRecursive(n.Children) : new List<TreeNode>()
            }).ToList();
        }

        private List<TreeNode> BuildTreeNodesRecursive(List<TreeNodeResponse> nodes)
        {
            return nodes.Select(n => new TreeNode
            {
                Name = n.Name,
                Path = n.Path,
                IsExpanded = n.IsExpanded,
                HasChildren = n.HasChildren,
                Children = n.Children != null ? BuildTreeNodesRecursive(n.Children) : new List<TreeNode>()
            }).ToList();
        }

        private async Task SetupKeyboardShortcuts()
        {
            await JSRuntime.InvokeVoidAsync("registerKeyboardShortcuts",
                DotNetObjectReference.Create(this));
        }

        [JSInvokable]
        public async Task HandleKeyboardShortcut(string shortcut)
        {
            switch (shortcut)
            {
                case "ctrl+a":
                    SelectAll();
                    break;
                case "delete":
                    await DeleteSelected();
                    break;
                case "ctrl+c":
                    await CopySelected();
                    break;
                case "ctrl+v":
                    await Paste();
                    break;
                case "f2":
                    await RenameSelected();
                    break;
                case "f5":
                    await RefreshCurrentView();
                    break;
            }
        }

        private async Task CopySelected()
        {
            // Implement copy logic
            LastOperation = $"Copied {SelectedItems.Count} items";
        }

        private async Task Paste()
        {
            // Implement paste logic
            LastOperation = "Paste operation";
        }

        private async Task RenameSelected()
        {
            if (SelectedItems.Count != 1) return;

            var item = SelectedItems.First();
            var dialog = await DialogService.ShowInputAsync("Rename", "Enter new name:", item.Name);

            if (!string.IsNullOrEmpty(dialog.Value) && dialog.Value != item.Name)
            {
                try
                {
                    await ApiClient.RenameAsync(item.Id, dialog.Value);
                    await RefreshCurrentView();
                    LastOperation = $"Renamed to {dialog.Value}";
                }
                catch (Exception ex)
                {
                    await ShowErrorDialog("Rename Failed", ex.Message);
                }
            }
        }

        private async Task ShowErrorDialog(string title, string message)
        {
            await DialogService.ShowErrorAsync(title, message);
        }

        private void OnStateChanged(object sender, EventArgs e)
        {
            // Handle global state changes from Fluxor
            InvokeAsync(StateHasChanged);
        }

        #endregion

        #region Nested Classes

        private class BreadcrumbItem
        {
            public string Name { get; set; }
            public string Path { get; set; }
        }

        private class TreeNode
        {
            public string Name { get; set; }
            public string Path { get; set; }
            public bool IsExpanded { get; set; }
            public bool HasChildren { get; set; }
            public List<TreeNode> Children { get; set; } = new();
        }

        #endregion
    }

    public enum ViewMode
    {
        Grid,
        List,
        Details
    }
}

