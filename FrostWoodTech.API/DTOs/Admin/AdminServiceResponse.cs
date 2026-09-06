namespace FrostWoodTech.API.DTOs.Admin;

/// <summary>Admin view: both sites' visibility, the draft flag and audit metadata.</summary>
public sealed class AdminServiceResponse
{
    public required Guid Id { get; init; }

    public required string Slug { get; init; }

    public required string Name { get; init; }

    public required string ShortDescription { get; init; }

    public string? Eyebrow { get; init; }

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

    public string? IconObjectKey { get; init; }

    public string? IconUrl { get; init; }

    public int? IconWidth { get; init; }

    public int? IconHeight { get; init; }

    public string? IconAltText { get; init; }

    public string? HeroImageObjectKey { get; init; }

    public string? HeroImageUrl { get; init; }

    public int? HeroImageWidth { get; init; }

    public int? HeroImageHeight { get; init; }

    public string? HeroImageAltText { get; init; }

    public string? DepthImageObjectKey { get; init; }

    public string? DepthImageUrl { get; init; }

    public int? DepthImageWidth { get; init; }

    public int? DepthImageHeight { get; init; }

    public string? DepthImageAltText { get; init; }

    /// <summary>Linked case studies, oldest link order irrelevant — sorted by year descending.</summary>
    public required List<ServiceProjectSummary> Projects { get; init; }

    public string? SeoTitle { get; init; }

    public string? SeoDescription { get; init; }

    public required bool IsPublished { get; init; }

    public DateTimeOffset? PublishedAt { get; init; }

    public required bool ShowOnAgency { get; init; }

    public required bool FeaturedOnAgency { get; init; }

    public required int AgencySortOrder { get; init; }

    public required bool ShowOnPersonal { get; init; }

    public required bool FeaturedOnPersonal { get; init; }

    public required int PersonalSortOrder { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }
}
