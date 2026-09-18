using FrostWoodTech.API.Entities.Common;

namespace FrostWoodTech.API.Entities;

/// <summary>Visitor testimonial; unpublished until an admin publishes it.</summary>
public class Review : AuditableEntity
{
    public required string Name { get; set; }

    public required string Country { get; set; }

    public required string CountryCode { get; set; }

    public string? Position { get; set; }

    /// <summary>1-5, enforced by a check constraint.</summary>
    public int Rating { get; set; }

    public required string ReviewText { get; set; }

    public bool IsPublished { get; set; }

    public bool IsFeatured { get; set; }

    public int SortOrder { get; set; }

    public string? SubmitterIp { get; set; }
}
