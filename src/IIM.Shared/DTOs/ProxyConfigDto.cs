// IIM.Shared/DTOs/ProxyConfigDto.cs

/// <summary>
/// DTO for specifying proxy configuration details (type, host, port).
/// Used to pass proxy settings from UI to backend services.
/// </summary>
public class ProxyConfigDto
{
    /// <summary>
    /// Proxy type (e.g., "socks5h", "http", "https").
    /// </summary>
    public string ProxyType { get; set; } = "socks5h";
    /// <summary>
    /// Proxy host IP address or DNS name.
    /// </summary>
    public string Host { get; set; } = "127.0.0.1";
    /// <summary>
    /// Proxy port number.
    /// </summary>
    public int Port { get; set; } = 9050;
}
