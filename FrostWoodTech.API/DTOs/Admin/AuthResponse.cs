using System.Text.Json.Serialization;

namespace FrostWoodTech.API.DTOs.Admin;

/// <summary>
/// A short-lived access token plus the rotating refresh token that renews it. The refresh token
/// never reaches the response body — the Function that mints this reads <see cref="RefreshToken"/>
/// in C# to set it as an httpOnly cookie, and <see cref="JsonIgnoreAttribute"/> keeps it (and its
/// expiry) out of the serialized JSON the SPA actually receives.
/// </summary>
public sealed class AuthResponse
{
    public required string AccessToken { get; init; }

    public required DateTimeOffset ExpiresAt { get; init; }

    /// <summary>
    /// Raw value — it is never stored, only its hash is. Never serialized; cookie-only.
    /// Not `required`: System.Text.Json can't enforce "required" on an ignored property, so this
    /// still has to be set explicitly by <c>IssueTokensAsync</c> — the compiler just won't check it.
    /// </summary>
    [JsonIgnore]
    public string RefreshToken { get; init; } = "";

    [JsonIgnore]
    public DateTimeOffset RefreshTokenExpiresAt { get; init; }

    public required AdminUserResponse User { get; init; }
}
