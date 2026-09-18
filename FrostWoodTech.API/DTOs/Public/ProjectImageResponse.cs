namespace FrostWoodTech.API.DTOs.Public;

public sealed class ProjectImageResponse
{
    public required Guid Id { get; init; }

    public required string ObjectKey { get; init; }

    public required string Url { get; init; }

    public required string AltText { get; init; }

    public required int Width { get; init; }

    public required int Height { get; init; }

    public required bool IsPrimary { get; init; }

    public required int SortOrder { get; init; }
}
