namespace FrostWoodTech.API.DTOs.Admin;

public class CreateCertificateRequest
{
    public string? Name { get; set; }

    public string? IssuedBy { get; set; }

    public DateOnly IssuedDate { get; set; }

    /// <summary>Free text — a score, grade, or "Distinction" style result. Optional.</summary>
    public string? Marks { get; set; }

    public string? ObjectKey { get; set; }

    public string? Url { get; set; }

    public string? MimeType { get; set; }

    public int? Width { get; set; }

    public int? Height { get; set; }

    public string? AltText { get; set; }

    public bool IsPublished { get; set; }

    public bool Featured { get; set; }
}
