using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// src/IIM.Shared/Interfaces/IAuthenticationProvider.cs
namespace IIM.Shared.Interfaces;

public interface IAuthenticationProvider
{
    string ProviderName { get; }
    Task<AuthResult> AuthenticateAsync(Credentials credentials);
    Task<UserInfo> GetUserInfoAsync(string accessToken);
}

public record UserInfo(string Id, string DisplayName, string Email, List<string> Groups);
public record Credentials(string Username, string Password);
public record AuthResult(bool Succeeded, string Token = null, string ErrorMessage = null);
