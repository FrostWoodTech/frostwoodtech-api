namespace FrostWoodTech.API.DTOs.Public;

/// <summary>
/// What the personal site sees. The draft state and audit metadata never cross this boundary.
/// There is no `site` concept here at all — certificates only ever exist for the personal site.
/// </summary>
public sealed class CertificateResponse
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required string IssuedBy { get; init; }

    public required DateOnly IssuedDate { get; init; }

    public string? Marks { get; init; }

    public required string Url { get; init; }

    public required string MimeType { get; init; }

    public int? Width { get; init; }

    public int? Height { get; init; }

    public required string AltText { get; init; }

    public required bool Featured { get; init; }

    public required int SortOrder { get; init; }
}
