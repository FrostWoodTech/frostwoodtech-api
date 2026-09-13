using FrostWoodTech.API.Entities.Common;

namespace FrostWoodTech.API.Entities;

/// <summary>The services table; named to avoid clashing with the Services namespace.</summary>
public class ServiceOffering : SiteVisibleEntity
{
    public required string Slug { get; set; }

    public required string Name { get; set; }

    public required string ShortDescription { get; set; }

    public string? Eyebrow { get; set; }

    public string? Headline { get; set; }

    public string? Deck { get; set; }

    public string? WhoThisIsFor { get; set; }

    public string? Outcomes { get; set; }

    public string? Capabilities { get; set; }

    public string? InDepth { get; set; }

    public string? PrimaryCtaLabel { get; set; }

    public string? PrimaryCtaUrl { get; set; }

    public string? SecondaryCtaLabel { get; set; }

    public string? SecondaryCtaUrl { get; set; }

    /// <summary>Icon*, HeroImage* and DepthImage* groups are all-or-nothing, and alt text is required when set.</summary>
    public string? IconObjectKey { get; set; }

    public string? IconUrl { get; set; }

    public int? IconWidth { get; set; }

    public int? IconHeight { get; set; }

    public string? IconAltText { get; set; }

    public string? HeroImageObjectKey { get; set; }

    public string? HeroImageUrl { get; set; }

    public int? HeroImageWidth { get; set; }

    public int? HeroImageHeight { get; set; }

    public string? HeroImageAltText { get; set; }

    public string? DepthImageObjectKey { get; set; }

    public string? DepthImageUrl { get; set; }

    public int? DepthImageWidth { get; set; }

    public int? DepthImageHeight { get; set; }

    public string? DepthImageAltText { get; set; }

    public bool IsPublished { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }

    public string? SeoTitle { get; set; }

    public string? SeoDescription { get; set; }

    public ICollection<PricingPlan> PricingPlans { get; set; } = [];

    public ICollection<ServiceProject> ServiceProjects { get; set; } = [];

    public ICollection<Faq> Faqs { get; set; } = [];
}
