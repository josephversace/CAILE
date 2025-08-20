using IIM.Infrastructure.Platform;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IIM.Core.Mediator;

namespace IIM.Application.Commands.Wsl
{
    /// <summary>
    /// Handles ConfigureProxyCommand by saving proxy settings and invoking WSL/Tor setup logic.
    /// </summary>
    public class ConfigureProxyCommandHandler : IRequestHandler<ConfigureProxyCommand, Unit>
    {
        private readonly IWslManager _wslManager; // Service for WSL operations

        public ConfigureProxyCommandHandler(IWslManager wslManager)
        {
            _wslManager = wslManager;
        }

        /// <summary>
        /// Saves proxy config to a .env file, then invokes the platform layer to install/configure Tor via WSL.
        /// </summary>
        public async Task<Unit> Handle(ConfigureProxyCommand command, CancellationToken cancellationToken)
        {
            // Compose proxy environment variables in .env format
            var proxyEnv =
$@"http_proxy={command.ProxyConfig.ProxyType}://{command.ProxyConfig.Host}:{command.ProxyConfig.Port}
https_proxy={command.ProxyConfig.ProxyType}://{command.ProxyConfig.Host}:{command.ProxyConfig.Port}
ftp_proxy={command.ProxyConfig.ProxyType}://{command.ProxyConfig.Host}:{command.ProxyConfig.Port}
no_proxy=localhost,127.0.0.1,::1
";
            // Save to Windows temp folder (shared with WSL)
            var windowsPath = Path.Combine(Path.GetTempPath(), "proxy.env");
            await File.WriteAllTextAsync(windowsPath, proxyEnv, cancellationToken);

            // Call infrastructure service to install Tor and apply proxy config in WSL
            await _wslManager.InstallTorAndApplyProxyAsync(windowsPath, cancellationToken);

            // Optional: Raise domain event or notification here if needed

            return Unit.Value;
        }
    }
}
