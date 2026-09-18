namespace FrostWoodTech.API.DTOs.Admin;

public class AddPricingPlanFeatureRequest
{
    public string? Text { get; set; }

    public bool IsIncluded { get; set; }

    public int SortOrder { get; set; }
}
