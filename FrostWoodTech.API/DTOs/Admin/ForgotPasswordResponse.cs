namespace FrostWoodTech.API.DTOs.Admin;

/// <summary>Identical for every email, so the endpoint can't enumerate accounts.</summary>
public sealed class ForgotPasswordResponse
{
    public string Message { get; init; } = "If that account exists, we've sent a password reset email.";
}
