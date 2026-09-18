namespace FrostWoodTech.API.DTOs.Admin;

public sealed class ServiceProjectSummary
{
    public required Guid Id { get; init; }

    public required string Slug { get; init; }

    public required string Title { get; init; }

    public required int Year { get; init; }

    public required bool IsPublished { get; init; }
}
