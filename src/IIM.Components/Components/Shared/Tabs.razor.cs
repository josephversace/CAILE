using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace IIM.Components.Shared;

public partial class Tabs : ComponentBase
{
    internal readonly List<TabItem> _tabs = new();
    protected readonly HashSet<string> _everActivated = new();

    [Parameter] public string? ActiveTabId { get; set; }
    [Parameter] public EventCallback<string?> ActiveTabIdChanged { get; set; }

    // Renamed from "Lazy" → "IsLazy" to avoid confusion with System.Lazy<T>
    [Parameter] public bool IsLazy { get; set; } = true;

    [Parameter] public EventCallback<string?> OnTabChanged { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }

    internal void Register(TabItem tab)
    {
        if (!_tabs.Any(t => t.Id == tab.Id))
        {
            _tabs.Add(tab);
            if (string.IsNullOrWhiteSpace(ActiveTabId))
            {
                ActiveTabId = tab.Id;
                _everActivated.Add(tab.Id);
            }
            StateHasChanged();
        }
    }

    internal void Unregister(TabItem tab)
    {
        _tabs.Remove(tab);
        if (ActiveTabId == tab.Id)
        {
            ActiveTabId = _tabs.FirstOrDefault()?.Id;
            if (ActiveTabId is not null) _everActivated.Add(ActiveTabId);
        }
        StateHasChanged();
    }

    protected async Task Activate(string id)
    {
        if (ActiveTabId == id) return;
        ActiveTabId = id;
        _everActivated.Add(id);
        await ActiveTabIdChanged.InvokeAsync(id);
        await OnTabChanged.InvokeAsync(id);
        StateHasChanged();
    }

    protected async Task OnKeyDown(KeyboardEventArgs e, string currentId)
    {
        if (e.Key is not ("ArrowLeft" or "ArrowRight")) return;

        var index = _tabs.FindIndex(t => t.Id == currentId);
        if (index < 0 || _tabs.Count == 0) return;

        var next = e.Key == "ArrowRight"
            ? (index + 1) % _tabs.Count
            : (index - 1 + _tabs.Count) % _tabs.Count;

        await Activate(_tabs[next].Id);
    }

    internal sealed class TabItem
    {
        public string Id { get; init; } = Guid.NewGuid().ToString("N");
        public string Title { get; init; } = "";
        public string? Icon { get; init; }
        public bool Disabled { get; init; }
        public RenderFragment? ChildContent { get; init; }
    }
}
