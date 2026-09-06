namespace FrostWoodTech.API.DTOs.Admin;

/// <summary>Admin view: draft state and audit metadata included.</summary>
public sealed class AdminCertificateResponse
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required string IssuedBy { get; init; }

    public required DateOnly IssuedDate { get; init; }

    public string? Marks { get; init; }

    public required string ObjectKey { get; init; }

    public required string Url { get; init; }

    public required string MimeType { get; init; }

    public int? Width { get; init; }

    public int? Height { get; init; }

    public required string AltText { get; init; }

    public required bool IsPublished { get; init; }

    public required bool Featured { get; init; }

    /// <summary>Drag-and-drop order — never a typed number on create/update.</summary>
    public required int SortOrder { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }
}
