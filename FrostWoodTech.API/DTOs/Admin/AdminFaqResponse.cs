namespace FrostWoodTech.API.DTOs.Admin;

/// <summary>Admin view: both sites' visibility, the draft flag and audit metadata.</summary>
public sealed class AdminFaqResponse
{
    public required Guid Id { get; init; }

    /// <summary>Null means this is a general FAQ.</summary>
    public Guid? ServiceId { get; init; }

    /// <summary>For display next to the scope filter — null when <see cref="ServiceId"/> is null.</summary>
    public string? ServiceName { get; init; }

    public required string Question { get; init; }

    public required string Answer { get; init; }

    /// <summary>Shared by both sites — FAQs don't keep a per-site order.</summary>
    public required int SortOrder { get; init; }

    public required bool IsPublished { get; init; }

    public required bool ShowOnAgency { get; init; }

    public required bool ShowOnPersonal { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }
}
