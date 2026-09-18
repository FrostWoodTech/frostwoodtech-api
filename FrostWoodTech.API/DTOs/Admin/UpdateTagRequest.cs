using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.DTOs.Admin;

public sealed class UpdateTagRequest
{
    public string? Name { get; set; }

    public string? Slug { get; set; }

    public bool IsTechnology { get; set; }

    public TechCategory? TechnologyCategory { get; set; }
}
