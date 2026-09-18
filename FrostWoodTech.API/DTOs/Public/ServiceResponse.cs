namespace FrostWoodTech.API.DTOs.Public;

/// <summary>Public shape: Featured and SortOrder are for the requested site; no admin fields.</summary>
public sealed class ServiceResponse
{
    public required Guid Id { get; init; }

    public required string Slug { get; init; }

    public required string Name { get; init; }

    public required string ShortDescription { get; init; }

    public string? Eyebrow { get; init; }

    /// <summary>Null means fall back to Name.</summary>
    public string? Headline { get; init; }

    public string? Deck { get; init; }

    public string? WhoThisIsFor { get; init; }

    public string? Outcomes { get; init; }

    public string? Capabilities { get; init; }

    public string? InDepth { get; init; }

    public string? PrimaryCtaLabel { get; init; }

    public string? PrimaryCtaUrl { get; init; }

    public string? SecondaryCtaLabel { get; init; }

    public string? SecondaryCtaUrl { get; init; }

    public string? IconUrl { get; init; }

    public int? IconWidth { get; init; }

    public int? IconHeight { get; init; }

    public string? IconAltText { get; init; }

    public string? HeroImageUrl { get; init; }

    public int? HeroImageWidth { get; init; }

    public int? HeroImageHeight { get; init; }

    public string? HeroImageAltText { get; init; }

    public string? DepthImageUrl { get; init; }

    public int? DepthImageWidth { get; init; }

    public int? DepthImageHeight { get; init; }

    public string? DepthImageAltText { get; init; }

    public string? SeoTitle { get; init; }

    public string? SeoDescription { get; init; }

    /// <summary>Detail endpoint only; null on the list.</summary>
    public List<ServiceProjectResponse>? Projects { get; init; }

    /// <summary>Detail endpoint only; null on the list.</summary>
    public List<FaqResponse>? Faqs { get; init; }

    public DateTimeOffset? PublishedAt { get; init; }

    public required bool Featured { get; init; }

    public required int SortOrder { get; init; }
}
