using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.DTOs.Admin;

public sealed class CreateTagRequest
{
    public string? Name { get; set; }

    /// <summary>Optional — generated from the name when omitted.</summary>
    public string? Slug { get; set; }

    public bool IsTechnology { get; set; }

    public TechCategory? TechnologyCategory { get; set; }
}
