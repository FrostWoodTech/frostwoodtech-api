namespace FrostWoodTech.API.DTOs.Admin;

/// <summary>
/// Bulk sort_order update for FAQs. Unlike <see cref="ReorderRequest"/> there is no site — FAQs
/// share a single order across both sites.
/// </summary>
public sealed class FaqReorderRequest
{
    public List<ReorderItem>? Items { get; set; }
}
