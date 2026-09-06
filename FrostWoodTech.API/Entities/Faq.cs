using FrostWoodTech.API.Entities.Common;

namespace FrostWoodTech.API.Entities;

/// <summary>
/// Unlike the other site-visible entities, an FAQ has no "featured" concept and keeps a single
/// <see cref="SortOrder"/> shared by both sites rather than one per site — so it carries its own
/// visibility flags instead of the shared <see cref="SiteVisibleEntity"/> block.
/// </summary>
public class Faq : AuditableEntity
{
    public required string Question { get; set; }

    /// <summary>Markdown.</summary>
    public required string Answer { get; set; }

    /// <summary>
    /// Null means a general FAQ — the shared list both public sites render. Otherwise the FAQ
    /// belongs to that service's page only. Same "null means general" convention as
    /// <see cref="PricingPlan.ServiceId"/>.
    /// </summary>
    public Guid? ServiceId { get; set; }

    public ServiceOffering? Service { get; set; }

    /// <summary>Shared by both sites, and ordered within a scope rather than across all FAQs.</summary>
    public int SortOrder { get; set; }

    public bool IsPublished { get; set; }

    public bool ShowOnAgency { get; set; }

    public bool ShowOnPersonal { get; set; }
}
