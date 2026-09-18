namespace FrostWoodTech.API.DTOs.Admin;

/// <summary>Identical for every email, so the endpoint can't enumerate accounts.</summary>
public sealed class ResendVerificationResponse
{
    public string Message { get; init; } = "If that account needs verification, we've sent a new email.";
}
