using FrostWoodTech.API.Entities.Common;

namespace FrostWoodTech.API.Entities;

/// <summary>
/// Personal-site only — there is no agency equivalent, so unlike <see cref="SiteVisibleEntity"/>
/// there is no site choice at all, just a plain <see cref="Featured"/> flag and one
/// <see cref="SortOrder"/>. The uploaded file can be a PDF or an image, so <see cref="Width"/>/
/// <see cref="Height"/> are nullable — a PDF has no dimensions.
/// </summary>
public class Certificate : AuditableEntity
{
    public required string Name { get; set; }

    public required string IssuedBy { get; set; }

    public DateOnly IssuedDate { get; set; }

    /// <summary>Free text — a score, grade, or "Distinction" style result. Optional.</summary>
    public string? Marks { get; set; }

    public required string ObjectKey { get; set; }

    public required string Url { get; set; }

    public required string MimeType { get; set; }

    public int? Width { get; set; }

    public int? Height { get; set; }

    public required string AltText { get; set; }

    public bool IsPublished { get; set; }

    public bool Featured { get; set; }

    /// <summary>Display order, set only via the drag-and-drop reorder endpoint — never a typed number.</summary>
    public int SortOrder { get; set; }
}
