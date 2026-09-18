namespace FrostWoodTech.API.DTOs.Admin;

public sealed class AdminFaqResponse
{
    public required Guid Id { get; init; }

    /// <summary>Null means a general FAQ.</summary>
    public Guid? ServiceId { get; init; }

    public string? ServiceName { get; init; }

    public required string Question { get; init; }

    public required string Answer { get; init; }

    /// <summary>One order shared by both sites.</summary>
    public required int SortOrder { get; init; }

    public required bool IsPublished { get; init; }

    public required bool ShowOnAgency { get; init; }

    public required bool ShowOnPersonal { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }
}
