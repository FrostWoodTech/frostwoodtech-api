namespace FrostWoodTech.API.Auth;

public sealed class GoogleOptions
{
    /// <summary>Every ID token must name this in aud, or tokens for other Google apps would be accepted.</summary>
    public string? ClientId { get; set; }
}
