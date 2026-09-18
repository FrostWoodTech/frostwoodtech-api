namespace FrostWoodTech.API.DTOs.Admin;

public sealed class TrashedItemResponse
{
    public required Guid Id { get; init; }

    public required string Label { get; init; }

    /// <summary>Null on rows deleted before the trash feature shipped.</summary>
    public DateTimeOffset? DeletedAt { get; init; }

    public Guid? DeletedBy { get; init; }

    public string? DeletedByEmail { get; init; }
}
