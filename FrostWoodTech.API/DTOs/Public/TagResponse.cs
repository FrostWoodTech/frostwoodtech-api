using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.DTOs.Public;

public sealed class TagResponse
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required string Slug { get; init; }

    public required bool IsTechnology { get; init; }

    public TechCategory? TechnologyCategory { get; init; }
}
