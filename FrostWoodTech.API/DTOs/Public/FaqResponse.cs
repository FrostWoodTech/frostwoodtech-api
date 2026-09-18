namespace FrostWoodTech.API.DTOs.Public;

public sealed class FaqResponse
{
    public required Guid Id { get; init; }

    public required string Question { get; init; }

    public required string Answer { get; init; }

    public required int SortOrder { get; init; }
}
