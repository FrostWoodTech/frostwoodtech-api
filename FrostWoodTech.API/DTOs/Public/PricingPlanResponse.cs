using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.DTOs.Public;

public sealed class PricingPlanResponse
{
    public required Guid Id { get; init; }

    /// <summary>Null for a combo pack.</summary>
    public Guid? ServiceId { get; init; }

    public required string Name { get; init; }

    public string? Tagline { get; init; }

    /// <summary>Null means "Custom / Contact us", not zero.</summary>
    public decimal? PriceAmount { get; init; }

    public required string Currency { get; init; }

    public required PriceType PriceType { get; init; }

    public string? DeliveryText { get; init; }

    public required string Description { get; init; }

    public required bool IsPopular { get; init; }

    public string? CtaLabel { get; init; }

    public string? CtaUrl { get; init; }

    public required bool Featured { get; init; }

    public required int SortOrder { get; init; }

    public required IReadOnlyList<PricingPlanFeatureResponse> Features { get; init; }
}
