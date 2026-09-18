namespace FrostWoodTech.API.DTOs.Public;

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
