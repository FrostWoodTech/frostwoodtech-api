namespace FrostWoodTech.API.DTOs.Public;

/// <summary>
/// Deliberately thin — an acknowledgement, not the row back. Echoing the submission would put
/// admin-only fields (status, submitter IP) on the public surface.
/// </summary>
public sealed class ContactSubmissionResponse
{
    public required Guid Id { get; init; }
}
