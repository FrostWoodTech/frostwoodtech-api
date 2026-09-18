namespace FrostWoodTech.API.DTOs.Admin;

public sealed class AdminReviewResponse
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required string Country { get; init; }

    public required string CountryCode { get; init; }

    public string? Position { get; init; }

    public required int Rating { get; init; }

    public required string ReviewText { get; init; }

    public required bool IsPublished { get; init; }

    public required bool IsFeatured { get; init; }

    public required int SortOrder { get; init; }

    public string? SubmitterIp { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }
}
