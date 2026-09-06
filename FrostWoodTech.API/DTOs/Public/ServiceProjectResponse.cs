namespace FrostWoodTech.API.DTOs.Public;

/// <summary>
/// A case-study card on a service page. Card fields only — the full project is a round trip to
/// <c>/api/public/projects/{slug}</c> away.
/// </summary>
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
