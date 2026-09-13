namespace FrostWoodTech.API.DTOs.Public;

public sealed class ServiceProjectResponse
{
    public required Guid Id { get; init; }

    public required string Slug { get; init; }

    public required string Title { get; init; }

    public required string ShortDescription { get; init; }

    public required int Year { get; init; }

    public string? ImageUrl { get; init; }

    public string? ImageAltText { get; init; }
}
