namespace FrostWoodTech.API.DTOs.Admin;

public sealed class ReorderRequest
{
    public List<ReorderItem>? Items { get; set; }
}

public sealed class ReorderItem
{
    public Guid Id { get; set; }

    public int SortOrder { get; set; }
}
