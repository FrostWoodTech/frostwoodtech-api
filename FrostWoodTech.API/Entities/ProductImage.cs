namespace FrostWoodTech.API.Entities;

public class ProductImage
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public Product Product { get; set; } = null!;

    public required string ObjectKey { get; set; }

    public required string Url { get; set; }

    public required string AltText { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    /// <summary>Exactly one per product (partial unique index).</summary>
    public bool IsPrimary { get; set; }

    public int SortOrder { get; set; }
}
