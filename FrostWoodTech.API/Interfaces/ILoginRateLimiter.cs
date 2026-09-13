using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.Interfaces;

/// <summary>Postgres-backed limits per email and per IP, counted separately per action.</summary>
public interface ILoginRateLimiter
{
    Task<bool> IsBlockedAsync(
        string email,
        string? ipAddress,
        AuthAttemptAction action,
        CancellationToken cancellationToken);

    /// <summary>Login records failures only; password reset records every request.</summary>
    Task RecordAttemptAsync(
        string email,
        string? ipAddress,
        AuthAttemptAction action,
        CancellationToken cancellationToken);

    Task ClearAsync(string email, AuthAttemptAction action, CancellationToken cancellationToken);
}
