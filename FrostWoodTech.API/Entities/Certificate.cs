using FrostWoodTech.API.Entities.Common;
using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.Entities;

/// <summary>Personal-site only. A PDF has no Width/Height.</summary>
public class Certificate : AuditableEntity
{
    public required string Name { get; set; }

    public required string IssuedBy { get; set; }

    public CertificateCategory Category { get; set; }

    public DateOnly IssuedDate { get; set; }

    public string? Marks { get; set; }

    public required string ObjectKey { get; set; }

    public required string Url { get; set; }

    public required string MimeType { get; set; }

    public int? Width { get; set; }

    public int? Height { get; set; }

    public required string AltText { get; set; }

    public bool IsPublished { get; set; }

    public bool Featured { get; set; }

    /// <summary>Changed only via the reorder endpoint.</summary>
    public int SortOrder { get; set; }
}
