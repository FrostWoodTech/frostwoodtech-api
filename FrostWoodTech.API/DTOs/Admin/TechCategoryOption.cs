using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.DTOs.Admin;

public sealed class TechCategoryOption
{
    public required TechCategory Value { get; init; }

    public required string Label { get; init; }
}
