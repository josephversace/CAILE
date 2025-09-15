using IIM.Shared.Mediator;
using IIM.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Application.Wsl
{
    // IIM.Application/Commands/Wsl/ConfigureProxyCommand.cs

    /// <summary>
    /// Command to configure the proxy and trigger Tor installation/setup via WSL.
    /// </summary>
    /// <param name="ProxyConfig">Proxy configuration parameters.</param>
    public record ConfigureProxyCommand(ProxyConfig ProxyConfig) : ICommand;

}
