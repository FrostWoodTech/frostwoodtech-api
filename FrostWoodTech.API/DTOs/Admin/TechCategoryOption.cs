using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.DTOs.Admin;

/// <summary>One entry in the fixed <see cref="TechCategory"/> list — value plus display label.</summary>
public sealed class TechCategoryOption
{
    public required TechCategory Value { get; init; }

    public required string Label { get; init; }
}
