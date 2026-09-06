namespace FrostWoodTech.API.DTOs.Admin;

public class CreateServiceRequest
{
    public string? Name { get; set; }

    /// <summary>Optional — generated from the name when omitted.</summary>
    public string? Slug { get; set; }

    /// <summary>Markdown — the card blurb.</summary>
    public string? ShortDescription { get; set; }

    /// <summary>Small badge above the page headline.</summary>
    public string? Eyebrow { get; set; }

    /// <summary>The page's H1. Falls back to the name when omitted.</summary>
    public string? Headline { get; set; }

    /// <summary>The paragraph under the headline.</summary>
    public string? Deck { get; set; }

    /// <summary>Markdown bullet list.</summary>
    public string? WhoThisIsFor { get; set; }

    /// <summary>Markdown bullet list.</summary>
    public string? Outcomes { get; set; }

    /// <summary>Markdown bullet list.</summary>
    public string? Capabilities { get; set; }

    /// <summary>Markdown — the long-form body.</summary>
    public string? InDepth { get; set; }

    /// <summary>Set together with <see cref="PrimaryCtaUrl"/>, or not at all.</summary>
    public string? PrimaryCtaLabel { get; set; }

    public string? PrimaryCtaUrl { get; set; }

    /// <summary>Set together with <see cref="SecondaryCtaUrl"/>, or not at all.</summary>
    public string? SecondaryCtaLabel { get; set; }

    public string? SecondaryCtaUrl { get; set; }

    /// <summary>Neon object key from a presigned upload. Set together with the other Icon* fields, or not at all.</summary>
    public string? IconObjectKey { get; set; }

    public string? IconUrl { get; set; }

    public int? IconWidth { get; set; }

    public int? IconHeight { get; set; }

    /// <summary>Required whenever an icon is set.</summary>
    public string? IconAltText { get; set; }

    /// <summary>Neon object key from a presigned upload. Set together with the other HeroImage* fields, or not at all.</summary>
    public string? HeroImageObjectKey { get; set; }

    public string? HeroImageUrl { get; set; }

    public int? HeroImageWidth { get; set; }

    public int? HeroImageHeight { get; set; }

    /// <summary>Required whenever a hero image is set.</summary>
    public string? HeroImageAltText { get; set; }

    /// <summary>Neon object key from a presigned upload. Set together with the other DepthImage* fields, or not at all.</summary>
    public string? DepthImageObjectKey { get; set; }

    public string? DepthImageUrl { get; set; }

    public int? DepthImageWidth { get; set; }

    public int? DepthImageHeight { get; set; }

    /// <summary>Required whenever a depth image is set.</summary>
    public string? DepthImageAltText { get; set; }

    /// <summary>
    /// The complete set of projects shown as this service's case studies — the links are replaced
    /// wholesale on save, so an omitted id is an unlink.
    /// </summary>
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
