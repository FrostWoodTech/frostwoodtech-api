using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

using FrostWoodTech.API.Auth;
using FrostWoodTech.API.Entities;
using FrostWoodTech.API.Enums;
using FrostWoodTech.API.Middleware;

namespace FrostWoodTech.Tests;

public class JwtAuthenticationMiddlewareTests
{
    private static readonly JwtTokenService Tokens = NewTokenService(new string('k', 64));

    [Theory]
    [InlineData("/api/public/projects")]
    [InlineData("/api/health")]
    [InlineData("/api/docs")]
    [InlineData("/api/cms/admin/auth/login")]
    [InlineData("/api/cms/admin/auth/google")]
    [InlineData("/api/cms/admin/auth/register")]
    [InlineData("/api/cms/admin/auth/refresh")]
    [InlineData("/api/cms/admin/auth/logout")]
    [InlineData("/api/cms/admin/auth/verify-email")]
    [InlineData("/api/cms/admin/auth/resend-verification")]
    [InlineData("/api/cms/admin/auth/forgot-password")]
    [InlineData("/api/cms/admin/auth/set-password")]
    public async Task Public_and_allow_listed_auth_routes_need_no_token(string path)
    {
        var (allowed, context, _) = await AuthenticateAsync(path, authorization: null);

        Assert.True(allowed);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Theory]
    [InlineData("/api/cms/admin/users")]
    [InlineData("/api/cms/admin/projects")]
    [InlineData("/API/CMS/ADMIN/PROJECTS")]
    [InlineData("/api/cms/admin/projects/")]
    [InlineData("/cms/admin/projects")]
    [InlineData("/api/cms/admin/auth/me")]
    [InlineData("/api/cms/admin/auth/change-password")]
    [InlineData("/api/cms/admin/auth/login/extra")]
    public async Task Admin_routes_without_a_token_are_401(string path)
    {
        var (allowed, context, _) = await AuthenticateAsync(path, authorization: null);

        Assert.False(allowed);
        await AssertProblemAsync(context, "unauthenticated");
    }

    [Theory]
    [InlineData("Basic abc123")]
    [InlineData("Bearer")]
    [InlineData("Bearer    ")]
    public async Task A_malformed_authorization_header_is_401(string header)
    {
        var (allowed, context, _) = await AuthenticateAsync("/api/cms/admin/projects", header);

        Assert.False(allowed);
        await AssertProblemAsync(context, "unauthenticated");
    }

    [Theory]
    [InlineData("garbage")]
    [InlineData("expired")]
    [InlineData("wrong-key")]
    [InlineData("tampered")]
    [InlineData("wrong-audience")]
    public async Task An_invalid_token_is_401(string kind)
    {
        var token = kind switch
        {
            "garbage" => "not.a.jwt",
            "expired" => NewTokenService(new string('k', 64), minutes: -5).CreateAccessToken(NewUser()).Token,
            "wrong-key" => NewTokenService(new string('x', 64)).CreateAccessToken(NewUser()).Token,
            "wrong-audience" => NewTokenService(new string('k', 64), audience: "someone-else").CreateAccessToken(NewUser()).Token,
            _ => Tamper(Tokens.CreateAccessToken(NewUser()).Token)
        };

        var (allowed, context, currentUser) = await AuthenticateAsync("/api/cms/admin/projects", $"Bearer {token}");

        Assert.False(allowed);
        await AssertProblemAsync(context, "invalid_token");
        Assert.False(currentUser.IsAuthenticated);
    }

    [Fact]
    public async Task A_valid_token_fills_the_current_user()
    {
        var user = NewUser();
        var token = Tokens.CreateAccessToken(user).Token;

        var (allowed, _, currentUser) = await AuthenticateAsync("/api/cms/admin/users", $"Bearer {token}");

        Assert.True(allowed);
        Assert.Equal(user.Id, currentUser.UserId);
        Assert.Equal(user.Email, currentUser.Email);
        Assert.Equal(UserRole.SuperAdmin, currentUser.Role);
    }

    private static async Task<(bool Allowed, DefaultHttpContext Context, CurrentUser CurrentUser)> AuthenticateAsync(
        string path, string? authorization)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();

        if (authorization is not null)
        {
            context.Request.Headers.Authorization = authorization;
        }

        var currentUser = new CurrentUser();
        var allowed = await JwtAuthenticationMiddleware.AuthenticateAsync(context, Tokens, currentUser);

        return (allowed, context, currentUser);
    }

    private static async Task AssertProblemAsync(DefaultHttpContext context, string code)
    {
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);

        context.Response.Body.Position = 0;
        using var body = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal(code, body.RootElement.GetProperty("code").GetString());
    }

    private static string Tamper(string token)
    {
        // Flip a payload character so the signature no longer matches.
        var parts = token.Split('.');
        parts[1] = (parts[1][0] == 'A' ? 'B' : 'A') + parts[1][1..];

        return string.Join('.', parts);
    }

    private static JwtTokenService NewTokenService(string signer, int minutes = 15, string audience = "frostwoodtech-admin") =>
        new(Options.Create(new JwtOptions { Signer = signer, AccessTokenMinutes = minutes, Audience = audience }));

    private static User NewUser() => new()
    {
        Id = Guid.NewGuid(),
        Email = "root@example.com",
        FirstName = "Root",
        LastName = "Admin",
        Role = UserRole.SuperAdmin,
        Status = UserStatus.Approved
    };
}
