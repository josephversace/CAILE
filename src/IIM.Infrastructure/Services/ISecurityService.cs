// IIM.Core/Services/ISecurityService.cs
using Microsoft.Extensions.Logging;
using IIM.Core.Services;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;

namespace IIM.Infrastructure.Services;


// Basic implementation
public class SecurityService : ISecurityService
{
    private readonly ILogger<SecurityService> _logger;

    public SecurityService(ILogger<SecurityService> logger)
    {
        _logger = logger;
    }

    public UserInfo GetCurrentUser()
    {
        // In production, get from authentication context
        return new UserInfo(
               Id: "system",
               DisplayName: "System User",
               Email: "system@localhost",
               Groups: new List<string> { "System" }
           );
    }

    public async Task<string> GenerateHashAsync(string content)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        var hash = await Task.Run(() => sha256.ComputeHash(bytes));
        return Convert.ToHexString(hash);
    }

    public bool VerifyHash(string content, string hash)
    {
        var computedHash = GenerateHashAsync(content).Result;
        return string.Equals(computedHash, hash, StringComparison.OrdinalIgnoreCase);
    }

    public async Task<bool> HasPermissionAsync(string userId, string permission)
    {
        // Implement permission checking
        return true;
    }

    public string GetCurrentUsername()
    {
        var user = GetCurrentUser();
        return user.DisplayName; // Use DisplayName instead of Username
    }

    public List<string> GetCurrentUserRoles()
    {
        var user = GetCurrentUser();
        return user.Groups; // Use Groups instead of Roles
    }

}