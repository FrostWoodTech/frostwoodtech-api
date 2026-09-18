using FrostWoodTech.API.Entities.Common;
using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.Entities;

/// <summary>Service tiers and combo packs. Agency-only, so no site visibility block.</summary>
public class PricingPlan : AuditableEntity
{
    /// <summary>Null means a combo pack.</summary>
    public Guid? ServiceId { get; set; }

    public ServiceOffering? Service { get; set; }

    public required string Name { get; set; }

    public string? Tagline { get; set; }

    /// <summary>Null means "Custom / Contact us"; never defaulted to 0.</summary>
    public decimal? PriceAmount { get; set; }

    public required string Currency { get; set; }

    public PriceType PriceType { get; set; }

    public string? DeliveryText { get; set; }

    public required string Description { get; set; }

    public bool IsPopular { get; set; }

    public string? CtaLabel { get; set; }

    public string? CtaUrl { get; set; }

    public bool IsPublished { get; set; }

    public bool Featured { get; set; }

    /// <summary>Changed only via the reorder endpoint.</summary>
    public int SortOrder { get; set; }

    public ICollection<PricingPlanFeature> Features { get; set; } = [];
}
