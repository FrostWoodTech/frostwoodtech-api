using FrostWoodTech.API.Entities.Common;
using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.Entities;

/// <summary>
/// Per-service tiers and general combo packs share this one table.
/// <see cref="ServiceId"/> = null means combo pack. Agency-only — unlike the other content
/// tables, pricing never appears on the personal site, so this carries its own
/// <see cref="Featured"/>/<see cref="SortOrder"/> instead of the shared
/// <see cref="SiteVisibleEntity"/> block.
/// </summary>
public class PricingPlan : AuditableEntity
{
    /// <summary>Null means a general / combo package.</summary>
    public Guid? ServiceId { get; set; }

    public ServiceOffering? Service { get; set; }

    public required string Name { get; set; }

    public string? Tagline { get; set; }

    /// <summary>Null means "Custom / Contact us" — never defaulted to 0.</summary>
    public decimal? PriceAmount { get; set; }

    /// <summary>ISO 4217, e.g. 'LKR', 'USD'.</summary>
    public required string Currency { get; set; }

    public PriceType PriceType { get; set; }

    /// <summary>Free text for ranges such as "2–3 weeks".</summary>
    public string? DeliveryText { get; set; }

    public required string Description { get; set; }

    /// <summary>The highlighted middle card.</summary>
    public bool IsPopular { get; set; }

    public string? CtaLabel { get; set; }

    public string? CtaUrl { get; set; }

    public bool IsPublished { get; set; }

    /// <summary>Home page card.</summary>
    public bool Featured { get; set; }

    /// <summary>
    /// Display order, set only via the drag-and-drop reorder endpoint — never a typed number on
    /// create/update.
    /// </summary>
    public int SortOrder { get; set; }

    public ICollection<PricingPlanFeature> Features { get; set; } = [];
}
