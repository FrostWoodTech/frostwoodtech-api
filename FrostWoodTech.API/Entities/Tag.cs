using FrostWoodTech.API.Entities.Common;
using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.Entities;

/// <summary>
/// One table for both project categories and technologies. The two are told apart by
/// <see cref="IsTechnology"/>, not by a separate column — group them at read time.
/// </summary>
public class Tag : AuditableEntity
{
    public required string Name { get; set; }

    public required string Slug { get; set; }

    public bool IsTechnology { get; set; }

    /// <summary>Required when <see cref="IsTechnology"/> is true, otherwise must be null.</summary>
    public TechCategory? TechnologyCategory { get; set; }

    public ICollection<ProjectTag> ProjectTags { get; set; } = [];

    public ICollection<ArticleTag> ArticleTags { get; set; } = [];
}
