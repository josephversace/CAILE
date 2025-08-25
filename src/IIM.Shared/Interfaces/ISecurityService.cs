// IIM.Core/Services/ISecurityService.cs
using IIM.Shared.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IIM.Shared.Interfaces;

public interface ISecurityService
{
    UserInfo GetCurrentUser();
    Task<string> GenerateHashAsync(string content);
    bool VerifyHash(string content, string hash);
    Task<bool> HasPermissionAsync(string userId, string permission);
}


