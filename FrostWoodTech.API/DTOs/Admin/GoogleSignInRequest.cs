namespace FrostWoodTech.API.DTOs.Admin;

public sealed class GoogleSignInRequest
{
    /// <summary>Google ID token; verified server-side against Google's keys.</summary>
    public string? IdToken { get; set; }
}
