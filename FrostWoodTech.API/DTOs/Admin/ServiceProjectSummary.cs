namespace FrostWoodTech.API.DTOs.Admin;

/// <summary>
/// A linked project as the admin service form needs it — enough to render a chip and say whether
/// the project is live, not the whole case study.
/// </summary>
public sealed class ServiceProjectSummary
{
    public required Guid Id { get; init; }

    public required string Slug { get; init; }

    public required string Title { get; init; }

    public required int Year { get; init; }

    public required bool IsPublished { get; init; }
}
