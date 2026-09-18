namespace FrostWoodTech.API.DTOs.Admin;

public sealed class PricingReorderRequest
{
    public List<ReorderItem>? Items { get; set; }
}
