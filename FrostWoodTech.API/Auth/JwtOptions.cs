namespace FrostWoodTech.API.Auth;

public sealed class JwtOptions
{
    public string Signer { get; set; } = string.Empty;

    public string Issuer { get; set; } = "frostwoodtech-api";

    public string Audience { get; set; } = "frostwoodtech-admin";

    /// <summary>Short on purpose: a JWT can't be recalled, so this bounds a revoked user's access.</summary>
    public int AccessTokenMinutes { get; set; } = 15;

    public int RefreshTokenDays { get; set; } = 30;
}
