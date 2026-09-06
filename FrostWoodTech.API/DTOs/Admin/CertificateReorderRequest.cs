namespace FrostWoodTech.API.DTOs.Admin;

/// <summary>
/// Bulk sort_order update for certificates. Unlike <see cref="ReorderRequest"/> there is no
/// site — certificates only ever exist for the personal site, so there is only one order to keep.
/// </summary>
public sealed class CertificateReorderRequest
{
    public List<ReorderItem>? Items { get; set; }
}
