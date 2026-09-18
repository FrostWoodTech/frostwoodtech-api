namespace FrostWoodTech.API.DTOs.Admin;

public sealed class CertificateReorderRequest
{
    public List<ReorderItem>? Items { get; set; }
}
