namespace FrostWoodTech.API.Entities;

public class ProjectImage
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public required string ObjectKey { get; set; }

    public required string Url { get; set; }

    public required string AltText { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    /// <summary>Exactly one per project (partial unique index).</summary>
    public bool IsPrimary { get; set; }

    public int SortOrder { get; set; }
}
