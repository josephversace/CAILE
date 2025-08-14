using System;
using System.Collections.Generic;
using System.Globalization;

namespace IIM.Plugin.SDK;

internal static class PluginSecurity
{
    // true if the URL's host is the same as, or a subdomain of, allowedDomain
    internal static bool IsAllowedDomain(string url, string allowedDomain)
    {
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(allowedDomain))
            return false;

        var uri = new Uri(url, UriKind.Absolute);
        var idn = new IdnMapping();
        var host = idn.GetAscii(uri.Host).ToLowerInvariant();
        var allowed = idn.GetAscii(allowedDomain).ToLowerInvariant();

        if (host == allowed) return true;
        return host.EndsWith("." + allowed, StringComparison.Ordinal);
    }

    // single-domain guard
    internal static void EnsureAllowedDomain(string url, string allowedDomain)
    {
        if (!IsAllowedDomain(url, allowedDomain))
            throw new InvalidOperationException($"Domain not allowed for URL: {url}");
    }

    // multi-domain guard (this is what your base class calls)
    internal static void EnsureAllowedDomain(string url, IEnumerable<string> allowedDomains)
    {
        foreach (var d in allowedDomains)
            if (IsAllowedDomain(url, d)) return;

        throw new InvalidOperationException($"Domain not allowed for URL: {url}");
    }
}
