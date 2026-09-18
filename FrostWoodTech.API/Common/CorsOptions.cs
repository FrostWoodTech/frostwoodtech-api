namespace FrostWoodTech.API.Common;

public sealed class CorsOptions
{
    /// <summary>Comma-separated exact origins, no trailing slash. Empty disables CORS; never a wildcard.</summary>
    public string AllowedOrigins { get; set; } = "";

    public string[] Origins => AllowedOrigins
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
