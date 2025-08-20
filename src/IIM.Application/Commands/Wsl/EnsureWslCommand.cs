using IIM.Core.Mediator;
using System;
using System.Collections.Generic;

namespace IIM.Application.Commands.Wsl
{
    /// <summary>
    /// Command to ensure WSL2 is installed and configured with all required services
    /// </summary>
    public class EnsureWslCommand : ICommand
    {
        /// <summary>
        /// Automatically install WSL2 if not present
        /// </summary>
        public bool AutoInstall { get; set; } = true;

        /// <summary>
        /// Start all required services after installation
        /// </summary>
        public bool StartServices { get; set; } = true;

        /// <summary>
        /// Install the IIM Ubuntu distribution
        /// </summary>
        public bool InstallDistro { get; set; } = true;

        /// <summary>
        /// Distro name to use
        /// </summary>
        public string DistroName { get; set; } = "IIM-Ubuntu";

        /// <summary>
        /// Services to ensure are running
        /// </summary>
        public List<string> RequiredServices { get; set; } = new()
        {
            "qdrant",
            "embed",
            "ollama",
            "jupyterlab"
        };

        /// <summary>
        /// Timeout for the entire operation
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(10);
    }
}
