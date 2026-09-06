using FrostWoodTech.API.DTOs.Public;
using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.DTOs.Admin;

/// <summary>Admin view: the draft flag and audit metadata, in addition to the public fields.</summary>
public sealed class AdminPricingPlanResponse
{
    public required Guid Id { get; init; }

    /// <summary>Null for a combo pack.</summary>
    public Guid? ServiceId { get; init; }

    public required string Name { get; init; }

    public string? Tagline { get; init; }

    /// <summary>Null means "Custom / Contact us".</summary>
    public decimal? PriceAmount { get; init; }

    public required string Currency { get; init; }

    public required PriceType PriceType { get; init; }

    public string? DeliveryText { get; init; }

    public required string Description { get; init; }

    public required bool IsPopular { get; init; }

    public string? CtaLabel { get; init; }

    public string? CtaUrl { get; init; }

    public required bool IsPublished { get; init; }

    /// <summary>Home page card.</summary>
    public required bool Featured { get; init; }

    /// <summary>Display order, set only via the reorder endpoint.</summary>
    public required int SortOrder { get; init; }

    /// <summary>Features carry no admin-only fields, so the public shape is reused as is.</summary>
    public required IReadOnlyList<PricingPlanFeatureResponse> Features { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }
}
