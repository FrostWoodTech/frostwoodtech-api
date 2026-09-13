using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;

using FrostWoodTech.API.Auth;
using FrostWoodTech.API.Common;
using FrostWoodTech.API.Enums;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Middleware;

/// <summary>Protects every /api/cms/admin/* route, so new admin endpoints are covered automatically.</summary>
public sealed class JwtAuthenticationMiddleware : IFunctionsWorkerMiddleware
{
    private const string AdminPrefix = "/cms/admin/";

    // Allow-list, so a forgotten route fails closed.
    private static readonly string[] AnonymousAdminPaths =
    [
        "/cms/admin/auth/register",
        "/cms/admin/auth/login",
        "/cms/admin/auth/google",
        "/cms/admin/auth/refresh",
        "/cms/admin/auth/logout",
        "/cms/admin/auth/verify-email",
        "/cms/admin/auth/resend-verification",
        "/cms/admin/auth/forgot-password",
        "/cms/admin/auth/set-password"
    ];

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var httpContext = context.GetHttpContext();

        if (httpContext is null
            || await AuthenticateAsync(
                httpContext,
                context.InstanceServices.GetRequiredService<IJwtTokenService>(),
                context.InstanceServices.GetRequiredService<CurrentUser>()))
        {
            await next(context);
        }
    }

    /// <summary>True when the request may continue; otherwise a 401 has already been written.</summary>
    internal static async Task<bool> AuthenticateAsync(
        HttpContext httpContext,
        IJwtTokenService tokenService,
        CurrentUser currentUser)
    {
        var path = Normalise(httpContext.Request.Path.Value);

        if (!path.StartsWith(AdminPrefix, StringComparison.Ordinal)
            || AnonymousAdminPaths.Contains(path, StringComparer.Ordinal))
        {
            return true;
        }

        var token = ReadBearerToken(httpContext.Request);
        if (token is null)
        {
            await Unauthorized(httpContext, "unauthenticated", "An Authorization: Bearer token is required.");
            return false;
        }

        var validation = await new JsonWebTokenHandler()
            .ValidateTokenAsync(token, tokenService.CreateValidationParameters());

        if (!validation.IsValid
            || !Guid.TryParse(ReadClaim(validation.Claims, "sub"), out var userId))
        {
            await Unauthorized(httpContext, "invalid_token", "The access token is invalid or has expired.");
            return false;
        }

        currentUser.UserId = userId;
        currentUser.Email = ReadClaim(validation.Claims, "email");
        currentUser.Role = Enum.TryParse<UserRole>(ReadClaim(validation.Claims, "role"), out var role) ? role : null;

        return true;
    }

    // Routes are declared without the api/ prefix.
    private static string Normalise(string? path)
    {
        path = (path ?? string.Empty).TrimEnd('/').ToLowerInvariant();

        return path.StartsWith("/api/", StringComparison.Ordinal) ? path[4..] : path;
    }

    private static string? ReadBearerToken(HttpRequest request)
    {
        var header = request.Headers.Authorization.ToString();

        return header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? header["Bearer ".Length..].Trim() is { Length: > 0 } value ? value : null
            : null;
    }

    private static string? ReadClaim(IDictionary<string, object> claims, string name) =>
        claims.TryGetValue(name, out var value) ? value?.ToString() : null;

    private static Task Unauthorized(HttpContext httpContext, string code, string detail) =>
        ProblemResults.WriteAsync(httpContext.Response, StatusCodes.Status401Unauthorized, "Unauthorized", code, detail);
}
