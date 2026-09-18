namespace FrostWoodTech.API.DTOs.Public;

public sealed class PricingPlanFeatureResponse
{
    public required Guid Id { get; init; }

    public required string Text { get; init; }

    public required bool IsIncluded { get; init; }

    public required int SortOrder { get; init; }
}
