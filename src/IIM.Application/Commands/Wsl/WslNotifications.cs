using IIM.Core.Mediator;
using IIM.Infrastructure.Platform;
using System;
using System.Collections.Generic;

namespace IIM.Application.Commands.Wsl
{
    /// <summary>
    /// Notification when WSL feature is enabled
    /// </summary>
    public class WslFeatureEnabledNotification : INotification
    {
        public DateTimeOffset Timestamp { get; set; }
        public bool RequiresRestart { get; set; }
    }

    /// <summary>
    /// Notification when a WSL distro is installed
    /// </summary>
    public class WslDistroInstalledNotification : INotification
    {
        public string DistroName { get; set; } = string.Empty;
        public WslDistroState State { get; set; }
        public string Version { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }

    /// <summary>
    /// Notification when WSL setup is completed
    /// </summary>
    public class WslSetupCompletedNotification : INotification
    {
        public bool Success { get; set; }
        public string DistroName { get; set; } = string.Empty;
        public List<string> ServicesStarted { get; set; } = new();
        public DateTimeOffset Timestamp { get; set; }
    }

    /// <summary>
    /// Notification when WSL setup fails
    /// </summary>
    public class WslSetupFailedNotification : INotification
    {
        public string Error { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
    }
}