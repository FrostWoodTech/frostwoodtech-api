using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.DTOs.Admin;

/// <summary>Triage only: status and notes. The visitor's submission is never editable.</summary>
public sealed class UpdateContactSubmissionRequest
{
    public ContactSubmissionStatus Status { get; set; }

    public string? AdminNotes { get; set; }
}
