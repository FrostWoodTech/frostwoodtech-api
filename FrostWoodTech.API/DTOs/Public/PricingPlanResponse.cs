using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.DTOs.Public;

/// <summary>
/// One pricing card. A null <see cref="ServiceId"/> means a combo pack rather than a tier of a
/// single service. Agency-only.
/// </summary>
public sealed class PricingPlanResponse
{
    public required Guid Id { get; init; }

    /// <summary>Null for a combo pack.</summary>
    public Guid? ServiceId { get; init; }

    public required string Name { get; init; }

    public string? Tagline { get; init; }

    /// <summary>Null means "Custom / Contact us" — the frontend renders that, not a zero.</summary>
    public decimal? PriceAmount { get; init; }

    public required string Currency { get; init; }

    public required PriceType PriceType { get; init; }

    /// <summary>Free text for ranges such as "2–3 weeks".</summary>
    public string? DeliveryText { get; init; }

    public required string Description { get; init; }

    /// <summary>The highlighted middle card.</summary>
    public required bool IsPopular { get; init; }

    public string? CtaLabel { get; init; }

    public string? CtaUrl { get; init; }

    /// <summary>Home page card.</summary>
    public required bool Featured { get; init; }

    /// <summary>Display order, set only via drag-and-drop.</summary>
    public required int SortOrder { get; init; }

    public required IReadOnlyList<PricingPlanFeatureResponse> Features { get; init; }
}
