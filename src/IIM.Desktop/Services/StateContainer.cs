

/// <summary>
/// State container for managing UI state across Blazor components.
/// Implements the observer pattern for state change notifications.
/// </summary>
public class StateContainer
{
    private InvestigationSession? _currentSession;
    private readonly List<Notification> _notifications = new();

    /// <summary>
    /// Gets or sets the current investigation session.
    /// Raises OnChange event when modified.
    /// </summary>
    public InvestigationSession? CurrentSession
    {
        get => _currentSession;
        set
        {
            _currentSession = value;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Gets a read-only list of current notifications.
    /// </summary>
    public IReadOnlyList<Notification> Notifications => _notifications.AsReadOnly();

    /// <summary>
    /// Adds a new notification to the notification list.
    /// Raises OnChange event to update UI.
    /// </summary>
    /// <param name="notification">Notification to add</param>
    public void AddNotification(Notification notification)
    {
        _notifications.Add(notification);
        NotifyStateChanged();
    }

    /// <summary>
    /// Event raised when state changes.
    /// Subscribe to this event in Blazor components to refresh UI.
    /// </summary>
    public event Action? OnChange;

    /// <summary>
    /// Notifies all subscribers that state has changed.
    /// Triggers UI refresh in subscribed components.
    /// </summary>
    private void NotifyStateChanged() => OnChange?.Invoke();
}