using FrostWoodTech.API.Entities.Common;

namespace FrostWoodTech.API.Entities;

/// <summary>
/// The <c>services</c> table. Named <c>ServiceOffering</c> rather than <c>Service</c> so the type
/// does not collide with the <c>FrostWoodTech.API.Services</c> namespace.
/// </summary>
public class ServiceOffering : SiteVisibleEntity
{
    public required string Slug { get; set; }

    public required string Name { get; set; }

    /// <summary>Markdown — the card blurb, and the fallback body when the page fields are empty.</summary>
    public required string ShortDescription { get; set; }

    /// <summary>Small badge above the page headline, e.g. "Arizona · Website pages".</summary>
    public string? Eyebrow { get; set; }

    /// <summary>The page's H1. Falls back to <see cref="Name"/>, which is the shorter nav label.</summary>
    public string? Headline { get; set; }

    /// <summary>The paragraph under the headline.</summary>
    public string? Deck { get; set; }

    /// <summary>Markdown bullet list — "who this is for".</summary>
    public string? WhoThisIsFor { get; set; }

    /// <summary>Markdown bullet list — "what you walk away with".</summary>
    public string? Outcomes { get; set; }

    /// <summary>Markdown bullet list — "what you get".</summary>
    public string? Capabilities { get; set; }

    /// <summary>Markdown — the long-form "in depth" prose block.</summary>
    public string? InDepth { get; set; }

    /// <summary>Label and URL are set together or not at all.</summary>
    public string? PrimaryCtaLabel { get; set; }

    public string? PrimaryCtaUrl { get; set; }

    public string? SecondaryCtaLabel { get; set; }

    public string? SecondaryCtaUrl { get; set; }

    /// <summary>Neon object key. Null/empty together with the other Icon* fields — no icon set.</summary>
    public string? IconObjectKey { get; set; }

    public string? IconUrl { get; set; }

    public int? IconWidth { get; set; }

    public int? IconHeight { get; set; }

    /// <summary>Required whenever an icon is set.</summary>
    public string? IconAltText { get; set; }

    /// <summary>Neon object key. Null/empty together with the other HeroImage* fields — no hero image set.</summary>
    public string? HeroImageObjectKey { get; set; }

    public string? HeroImageUrl { get; set; }

    public int? HeroImageWidth { get; set; }

    public int? HeroImageHeight { get; set; }

    /// <summary>Required whenever a hero image is set.</summary>
    public string? HeroImageAltText { get; set; }

    /// <summary>Neon object key for the second, "in depth" image. Same all-or-nothing rule.</summary>
    public string? DepthImageObjectKey { get; set; }

    public string? DepthImageUrl { get; set; }

    public int? DepthImageWidth { get; set; }

    public int? DepthImageHeight { get; set; }

    /// <summary>Required whenever a depth image is set.</summary>
    public string? DepthImageAltText { get; set; }

    public bool IsPublished { get; set; }

    /// <summary>Stamped the first time the service goes live, never cleared.</summary>
    public DateTimeOffset? PublishedAt { get; set; }

    public string? SeoTitle { get; set; }

    public string? SeoDescription { get; set; }

    public ICollection<PricingPlan> PricingPlans { get; set; } = [];

    /// <summary>Case studies shown on the service page.</summary>
    public ICollection<ServiceProject> ServiceProjects { get; set; } = [];

    /// <summary>FAQs scoped to this service. The global list is the rows with a null service.</summary>
    public ICollection<Faq> Faqs { get; set; } = [];
}
