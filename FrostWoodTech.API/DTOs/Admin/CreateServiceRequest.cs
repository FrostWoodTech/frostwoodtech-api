namespace FrostWoodTech.API.DTOs.Admin;

public class CreateServiceRequest
{
    public string? Name { get; set; }

    /// <summary>Generated from the name when omitted.</summary>
    public string? Slug { get; set; }

    public string? ShortDescription { get; set; }

    public string? Eyebrow { get; set; }

    /// <summary>Page H1; falls back to the name.</summary>
    public string? Headline { get; set; }

    public string? Deck { get; set; }

    public string? WhoThisIsFor { get; set; }

    public string? Outcomes { get; set; }

    public string? Capabilities { get; set; }

    public string? InDepth { get; set; }

    /// <summary>Set together with PrimaryCtaUrl, or not at all.</summary>
    public string? PrimaryCtaLabel { get; set; }

    public string? PrimaryCtaUrl { get; set; }

    /// <summary>Set together with SecondaryCtaUrl, or not at all.</summary>
    public string? SecondaryCtaLabel { get; set; }

    public string? SecondaryCtaUrl { get; set; }

    public string? IconObjectKey { get; set; }

    public string? IconUrl { get; set; }

    public int? IconWidth { get; set; }

    public int? IconHeight { get; set; }

    /// <summary>Required whenever an icon is set.</summary>
    public string? IconAltText { get; set; }

    public string? HeroImageObjectKey { get; set; }

    public string? HeroImageUrl { get; set; }

    public int? HeroImageWidth { get; set; }

    public int? HeroImageHeight { get; set; }

    /// <summary>Required whenever a hero image is set.</summary>
    public string? HeroImageAltText { get; set; }

    public string? DepthImageObjectKey { get; set; }

    public string? DepthImageUrl { get; set; }

    public int? DepthImageWidth { get; set; }

    public int? DepthImageHeight { get; set; }

    /// <summary>Required whenever a depth image is set.</summary>
    public string? DepthImageAltText { get; set; }

    /// <summary>The full set of linked projects; an omitted id is unlinked.</summary>
    public List<Guid>? ProjectIds { get; set; }

    public string? SeoTitle { get; set; }

    public string? SeoDescription { get; set; }

    public bool IsPublished { get; set; }

    public bool ShowOnAgency { get; set; }

    public bool FeaturedOnAgency { get; set; }

    public int AgencySortOrder { get; set; }

    public bool ShowOnPersonal { get; set; }

    public bool FeaturedOnPersonal { get; set; }

    public int PersonalSortOrder { get; set; }
}
