using FrostWoodTech.API.Common;

namespace FrostWoodTech.API.Interfaces;

/// <param name="Subject">Google's stable user id (the sub claim).</param>
public sealed record GoogleIdentity(
    string Subject,
    string Email,
    bool EmailVerified,
    string? FirstName,
    string? LastName,
    string? AvatarUrl);

public interface IGoogleTokenValidator
{
    /// <summary>Checks signature, issuer and audience against Google's keys.</summary>
    Task<ServiceResult<GoogleIdentity>> ValidateAsync(string idToken, CancellationToken cancellationToken);
}
