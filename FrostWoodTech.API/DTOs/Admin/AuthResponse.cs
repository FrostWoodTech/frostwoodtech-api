using System.Text.Json.Serialization;

namespace FrostWoodTech.API.DTOs.Admin;

/// <summary>Refresh token is set as an httpOnly cookie by the Function and never serialized.</summary>
public sealed class AuthResponse
{
    public required string AccessToken { get; init; }

    public required DateTimeOffset ExpiresAt { get; init; }

    /// <summary>Raw token for the cookie; only its hash is stored.</summary>
    [JsonIgnore]
    public string RefreshToken { get; init; } = "";

    [JsonIgnore]
    public DateTimeOffset RefreshTokenExpiresAt { get; init; }

    public required AdminUserResponse User { get; init; }
}
