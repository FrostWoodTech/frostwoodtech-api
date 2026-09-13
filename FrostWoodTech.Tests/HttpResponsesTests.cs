using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using FrostWoodTech.API.Common;

namespace FrostWoodTech.Tests;

public class HttpResponsesTests
{
    private static readonly object Payload = new { items = new[] { "a", "b" }, total = 2 };

    [Fact]
    public void A_public_response_is_cacheable_and_carries_an_etag()
    {
        var context = new DefaultHttpContext();

        var result = HttpResponses.PublicJson(context.Request, Payload);

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal(StatusCodes.Status200OK, content.StatusCode);
        Assert.Equal("application/json", content.ContentType);
        Assert.Equal("public, max-age=300", context.Response.Headers.CacheControl.ToString());
        Assert.Matches("^\"[0-9a-f]{32}\"$", context.Response.Headers.ETag.ToString());
    }

    [Fact]
    public void The_same_payload_always_gets_the_same_etag()
    {
        Assert.Equal(ETagFor(Payload), ETagFor(new { items = new[] { "a", "b" }, total = 2 }));
        Assert.NotEqual(ETagFor(Payload), ETagFor(new { items = new[] { "a" }, total = 1 }));
    }

    [Theory]
    [InlineData("{0}")]
    [InlineData("W/{0}")]
    [InlineData("\"stale\", {0}")]
    [InlineData("*")]
    public void A_matching_if_none_match_returns_304(string headerFormat)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.IfNoneMatch = string.Format(headerFormat, ETagFor(Payload));

        var result = HttpResponses.PublicJson(context.Request, Payload);

        var status = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status304NotModified, status.StatusCode);
    }

    [Theory]
    [InlineData("\"stale\"")]
    [InlineData("not-a-valid-etag")]
    public void A_non_matching_if_none_match_returns_the_body(string header)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.IfNoneMatch = header;

        Assert.IsType<ContentResult>(HttpResponses.PublicJson(context.Request, Payload));
    }

    [Fact]
    public void Admin_responses_are_marked_no_store()
    {
        var context = new DefaultHttpContext();

        HttpResponses.MarkNoStore(context.Request);

        Assert.Equal("no-store", context.Response.Headers.CacheControl.ToString());
    }

    [Fact]
    public void The_refresh_cookie_is_http_only_secure_lax_and_scoped_to_auth_routes()
    {
        var context = new DefaultHttpContext();

        HttpResponses.SetRefreshTokenCookie(context.Request, "raw-token", DateTimeOffset.UtcNow.AddDays(30));

        var cookie = context.Response.Headers.SetCookie.ToString().ToLowerInvariant();
        Assert.StartsWith("refreshtoken=raw-token", cookie);
        Assert.Contains("httponly", cookie);
        Assert.Contains("secure", cookie);
        Assert.Contains("samesite=lax", cookie);
        Assert.Contains("path=/api/cms/admin/auth", cookie);
    }

    private static string ETagFor(object payload)
    {
        var context = new DefaultHttpContext();
        HttpResponses.PublicJson(context.Request, payload);

        return context.Response.Headers.ETag.ToString();
    }
}
