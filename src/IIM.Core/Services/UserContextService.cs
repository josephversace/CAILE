// ============================================
// File: src/IIM.Core/Services/UserContextService.cs
// Provides user context for audit logging
// ============================================

using IIM.Core.Configuration;
using IIM.Core.Services;
using IIM.Shared.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace IIM.Core.Services
{
    public interface IUserContext
    {
        string? GetCurrentUserId();
        string? GetCurrentUserName();
        string? GetIpAddress();
        string? GetSessionId();
        ClaimsPrincipal? GetCurrentUser();
    }

    public class UserContextService : IUserContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserContextService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string? GetCurrentUserId()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;
        }

        public string? GetCurrentUserName()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Name)?.Value
                ?? _httpContextAccessor.HttpContext?.User?.Identity?.Name;
        }

        public string? GetIpAddress()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return null;

            // Check for forwarded IP (behind proxy/load balancer)
            var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwarded))
            {
                return forwarded.Split(',').First().Trim();
            }

            // Check real IP header
            var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            // Fall back to connection IP
            return context.Connection.RemoteIpAddress?.ToString();
        }

        public string? GetSessionId()
        {
            return _httpContextAccessor.HttpContext?.Session?.Id;
        }

        public ClaimsPrincipal? GetCurrentUser()
        {
            return _httpContextAccessor.HttpContext?.User;
        }
    }
}

