using FrostWoodTech.API.Entities.Common;
using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.Entities;

/// <summary>Categories and technologies share this table, split by IsTechnology.</summary>
public class Tag : AuditableEntity
{
    public required string Name { get; set; }

    public required string Slug { get; set; }

    public bool IsTechnology { get; set; }

    /// <summary>Required for technology tags, null otherwise.</summary>
    public TechCategory? TechnologyCategory { get; set; }

    public ICollection<ProjectTag> ProjectTags { get; set; } = [];

    public ICollection<ArticleTag> ArticleTags { get; set; } = [];
}
