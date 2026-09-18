using System.Security.Cryptography;
using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace FrostWoodTech.API.Common;

public static class HttpResponses
{
    private const int PublicMaxAgeSeconds = 300;

    /// <summary>Public cache headers plus an ETag; returns 304 when If-None-Match matches.</summary>
    public static IActionResult PublicJson(HttpRequest request, object payload)
    {
        var json = JsonSerializer.Serialize(payload, JsonDefaults.Options);
        var etag = ComputeETag(json);

        var response = request.HttpContext.Response;
        response.Headers.CacheControl = $"public, max-age={PublicMaxAgeSeconds}";
        response.Headers.ETag = etag;

        if (MatchesIfNoneMatch(request, etag))
        {
            return new StatusCodeResult(StatusCodes.Status304NotModified);
        }

        return new ContentResult
        {
            Content = json,
            ContentType = "application/json",
            StatusCode = StatusCodes.Status200OK
        };
    }

    public static void MarkNoStore(HttpRequest request)
    {
        request.HttpContext.Response.Headers[HeaderNames.CacheControl] = "no-store";
    }

    // Scoped to auth routes; no Domain, since only this API's origin reads it.
    private const string RefreshCookieName = "refreshToken";
    private const string RefreshCookiePath = "/api/cms/admin/auth";

    public static void SetRefreshTokenCookie(HttpRequest request, string refreshToken, DateTimeOffset expiresAt)
    {
        request.HttpContext.Response.Cookies.Append(RefreshCookieName, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax,
            Path = RefreshCookiePath,
            Expires = expiresAt,
        });
    }

    public static void ClearRefreshTokenCookie(HttpRequest request)
    {
        request.HttpContext.Response.Cookies.Delete(RefreshCookieName, new CookieOptions
        {
            Path = RefreshCookiePath,
        });
    }

    public static string? ReadRefreshTokenCookie(HttpRequest request) =>
        request.Cookies[RefreshCookieName];

    /// <summary>Weak comparison per RFC 9110: lists, W/ tags and * all match.</summary>
    private static bool MatchesIfNoneMatch(HttpRequest request, string etag)
    {
        if (!EntityTagHeaderValue.TryParseList(request.Headers.IfNoneMatch, out var candidates))
        {
            return false;
        }

        var current = EntityTagHeaderValue.Parse(etag);

        return candidates.Any(c => c.Equals(EntityTagHeaderValue.Any) || c.Compare(current, useStrongComparison: false));
    }

    private static string ComputeETag(string json)
    {
        var hash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(json));

        return $"\"{Convert.ToHexString(hash)[..32].ToLowerInvariant()}\"";
    }
}
