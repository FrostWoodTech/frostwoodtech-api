namespace FrostWoodTech.API.DTOs.Public;

public sealed class ReviewResponse
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required string Country { get; init; }

    public required string CountryCode { get; init; }

    public string? Position { get; init; }

    public required int Rating { get; init; }

    public required string ReviewText { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
}
