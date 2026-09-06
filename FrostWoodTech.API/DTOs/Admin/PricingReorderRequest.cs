namespace FrostWoodTech.API.DTOs.Admin;

/// <summary>
/// Bulk sort_order update for pricing plans. Unlike <see cref="ReorderRequest"/> there is no
/// site — pricing is agency-only, so there is only one order to keep.
/// </summary>
public sealed class PricingReorderRequest
{
    public List<ReorderItem>? Items { get; set; }
}
