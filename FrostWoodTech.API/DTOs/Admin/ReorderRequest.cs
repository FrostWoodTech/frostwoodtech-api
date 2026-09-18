using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.DTOs.Admin;

/// <summary>Sort order is per site, so the site is required.</summary>
public sealed class ReorderRequest
{
    public Site? Site { get; set; }

    public List<ReorderItem>? Items { get; set; }
}

public sealed class ReorderItem
{
    public Guid Id { get; set; }

    public int SortOrder { get; set; }
}
