using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.DTOs.Admin;

public class CreatePricingPlanRequest
{
    /// <summary>Omit or send null for a combo pack.</summary>
    public Guid? ServiceId { get; set; }

    /// <summary>"Starter", "Growth", "Landing Page Combo".</summary>
    public string? Name { get; set; }

    public string? Tagline { get; set; }

    /// <summary>
    /// Null is meaningful — it renders as "Custom / Contact us" and is never defaulted to 0.
    /// </summary>
    public decimal? PriceAmount { get; set; }

    /// <summary>ISO 4217, e.g. 'LKR', 'USD'.</summary>
    public string? Currency { get; set; }

    public PriceType PriceType { get; set; }

    /// <summary>Free text for ranges such as "2–3 weeks".</summary>
    public string? DeliveryText { get; set; }

    public string? Description { get; set; }

    /// <summary>The highlighted middle card.</summary>
    public bool IsPopular { get; set; }

    public string? CtaLabel { get; set; }

    public string? CtaUrl { get; set; }

    public bool IsPublished { get; set; }

    /// <summary>Home page card.</summary>
    public bool Featured { get; set; }
}
