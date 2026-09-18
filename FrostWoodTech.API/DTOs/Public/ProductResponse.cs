namespace FrostWoodTech.API.DTOs.Public;

/// <summary>Public shape: Featured and SortOrder are for the requested site; no admin fields.</summary>
public sealed class ProductResponse
{
    public required Guid Id { get; init; }

    public required string Slug { get; init; }

    public required string Name { get; init; }

    public required string Tagline { get; init; }

    public required string Description { get; init; }

    public string? PriceDetails { get; init; }

    public string? ProductUrl { get; init; }

    public DateTimeOffset? PublishedAt { get; init; }

    public string? SeoTitle { get; init; }

    public string? SeoDescription { get; init; }

    public required bool Featured { get; init; }

    public required int SortOrder { get; init; }

    public required IReadOnlyList<ProductImageResponse> Images { get; init; }
}
