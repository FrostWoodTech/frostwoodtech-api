namespace FrostWoodTech.API.Entities.Common;

/// <summary>Per-site visibility flags; a row is never duplicated per site.</summary>
public abstract class SiteVisibleEntity : AuditableEntity
{
    public bool ShowOnAgency { get; set; }

    /// <summary>Requires ShowOnAgency.</summary>
    public bool FeaturedOnAgency { get; set; }

    public int AgencySortOrder { get; set; }

    public bool ShowOnPersonal { get; set; }

    /// <summary>Requires ShowOnPersonal.</summary>
    public bool FeaturedOnPersonal { get; set; }

    public int PersonalSortOrder { get; set; }
}
