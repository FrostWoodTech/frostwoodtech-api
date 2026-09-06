using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.DTOs.Public;

/// <summary>
/// What the two public frontends see. No audit fields, no object keys — those are admin-only.
/// </summary>
public sealed class TagResponse
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required string Slug { get; init; }

    public required bool IsTechnology { get; init; }

    public TechCategory? TechnologyCategory { get; init; }
}
