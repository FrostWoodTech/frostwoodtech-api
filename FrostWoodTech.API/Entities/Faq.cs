using FrostWoodTech.API.Entities.Common;

namespace FrostWoodTech.API.Entities;

/// <summary>No featured flag and one sort order for both sites, so no SiteVisibleEntity.</summary>
public class Faq : AuditableEntity
{
    public required string Question { get; set; }

    public required string Answer { get; set; }

    /// <summary>Null means a general FAQ; otherwise shown only on that service's page.</summary>
    public Guid? ServiceId { get; set; }

    public ServiceOffering? Service { get; set; }

    /// <summary>Ordered within its scope (global or one service).</summary>
    public int SortOrder { get; set; }

    public bool IsPublished { get; set; }

    public bool ShowOnAgency { get; set; }

    public bool ShowOnPersonal { get; set; }
}
