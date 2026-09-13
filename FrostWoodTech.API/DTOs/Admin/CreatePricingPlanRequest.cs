using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.DTOs.Admin;

public class CreatePricingPlanRequest
{
    /// <summary>Null for a combo pack.</summary>
    public Guid? ServiceId { get; set; }

    public string? Name { get; set; }

    public string? Tagline { get; set; }

    /// <summary>Null means "Custom / Contact us"; never defaulted to 0.</summary>
    public decimal? PriceAmount { get; set; }

    public string? Currency { get; set; }

    public PriceType PriceType { get; set; }

    public string? DeliveryText { get; set; }

    public string? Description { get; set; }

    public bool IsPopular { get; set; }

    public string? CtaLabel { get; set; }

    public string? CtaUrl { get; set; }

    public bool IsPublished { get; set; }

    public bool Featured { get; set; }
}
