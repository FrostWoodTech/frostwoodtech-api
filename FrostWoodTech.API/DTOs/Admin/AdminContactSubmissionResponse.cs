using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.DTOs.Admin;

/// <summary>Full admin view: everything the visitor sent, plus triage state and the submitter's
/// IP for spam moderation. Never reused on the public surface.</summary>
public sealed class AdminContactSubmissionResponse
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required string Email { get; init; }

    public string? Phone { get; init; }

    public string? Company { get; init; }

    public string? Subject { get; init; }

    public required string Message { get; init; }

    public Guid? ServiceId { get; init; }

    public string? ServiceName { get; init; }

    public ContactBudgetRange? BudgetRange { get; init; }

    public required Site Site { get; init; }

    public required ContactSubmissionStatus Status { get; init; }

    public string? AdminNotes { get; init; }

    public DateTimeOffset? RepliedAt { get; init; }

    public Guid? RepliedBy { get; init; }

    public string? SubmitterIp { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }
}
