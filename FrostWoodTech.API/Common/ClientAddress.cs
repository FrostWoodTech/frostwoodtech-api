using Microsoft.AspNetCore.Http;

namespace FrostWoodTech.API.Common;

public static class ClientAddress
{
    /// <summary>Client IP for rate limiting. X-Forwarded-For is spoofable, so never use this for authorization.</summary>
    public static string? Read(HttpRequest request)
    {
        var forwarded = request.Headers["X-Forwarded-For"].ToString();

        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            var first = forwarded.Split(',')[0].Trim();

            // Strip the port Azure appends ("10.0.0.1:52000" or "[::1]:52000").
            var lastColon = first.LastIndexOf(':');
            if (first.StartsWith('[') && first.IndexOf(']') is var close and > 0)
            {
                first = first[1..close];
            }
            else if (lastColon > 0 && !first.Contains("::", StringComparison.Ordinal) && first.Count(c => c == ':') == 1)
            {
                first = first[..lastColon];
            }

            if (first.Length > 0)
            {
                return first;
            }
        }

        return request.HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}
