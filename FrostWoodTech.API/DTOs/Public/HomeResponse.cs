namespace FrostWoodTech.API.DTOs.Public;

/// <summary>All home slices in one call, scoped to the requested site and published rows.</summary>
public sealed class HomeResponse
{
    public required IReadOnlyList<ProjectResponse> FeaturedProjects { get; init; }

    public required IReadOnlyList<ArticleResponse> FeaturedArticles { get; init; }

    public required IReadOnlyList<ServiceResponse> FeaturedServices { get; init; }

    /// <summary>Featured combo packs.</summary>
    public required IReadOnlyList<PricingPlanResponse> FeaturedPricingPlans { get; init; }

    /// <summary>Not filtered by featured.</summary>
    public required IReadOnlyList<FaqResponse> Faqs { get; init; }

    /// <summary>Not site-scoped: reviews are shared by both sites.</summary>
    public required IReadOnlyList<ReviewResponse> FeaturedReviews { get; init; }
}
