namespace FrostWoodTech.API.DTOs.Public;

/// <summary>
/// What the public frontends see. The site's own visibility flags are already resolved into
/// <see cref="Featured"/> and <see cref="SortOrder"/> — the other site's flags, the draft state
/// and the audit metadata never cross this boundary.
/// </summary>
public sealed class ServiceResponse
{
    public required Guid Id { get; init; }

    public required string Slug { get; init; }

    public required string Name { get; init; }

    /// <summary>Markdown — sanitised on render, not on write.</summary>
    public required string ShortDescription { get; init; }

    /// <summary>Small badge above the page headline.</summary>
    public string? Eyebrow { get; init; }

    /// <summary>The page's H1. Null means fall back to <see cref="Name"/>.</summary>
    public string? Headline { get; init; }

    public string? Deck { get; init; }

    /// <summary>Markdown bullet list.</summary>
    public string? WhoThisIsFor { get; init; }

    /// <summary>Markdown bullet list.</summary>
    public string? Outcomes { get; init; }

    /// <summary>Markdown bullet list.</summary>
    public string? Capabilities { get; init; }

    /// <summary>Markdown.</summary>
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

    /// <summary>
    /// Case studies for this service. Null on the list endpoint — the cards there show neither
    /// projects nor FAQs, and loading both per row would be a payload and N+1 cost for nothing.
    /// </summary>
    public List<ServiceProjectResponse>? Projects { get; init; }

    /// <summary>This service's own FAQs, in sort order. Null on the list endpoint, as above.</summary>
    public List<FaqResponse>? Faqs { get; init; }

    public DateTimeOffset? PublishedAt { get; init; }

    /// <summary>Featured on the requested site.</summary>
    public required bool Featured { get; init; }

    /// <summary>Sort order for the requested site.</summary>
    public required int SortOrder { get; init; }
}
