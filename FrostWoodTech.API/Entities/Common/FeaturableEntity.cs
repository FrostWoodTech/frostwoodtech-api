namespace FrostWoodTech.API.Entities.Common;

/// <summary>Home-page featuring and manual ordering for public content.</summary>
public abstract class FeaturableEntity : AuditableEntity
{
    public bool IsFeatured { get; set; }

    public int SortOrder { get; set; }
}
