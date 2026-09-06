using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.DTOs.Admin;

/// <summary>
/// Triage only — an admin may change status and leave internal notes, never rewrite what the
/// visitor actually submitted.
/// </summary>
public sealed class UpdateContactSubmissionRequest
{
    public ContactSubmissionStatus Status { get; set; }

    public string? AdminNotes { get; set; }
}
