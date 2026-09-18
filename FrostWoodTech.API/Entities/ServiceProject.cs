namespace FrostWoodTech.API.Entities;

/// <summary>Service-to-project join; named to avoid clashing with ProjectService.</summary>
public class ServiceProject
{
    public Guid ServiceId { get; set; }

    public ServiceOffering Service { get; set; } = null!;

    public Guid ProjectId { get; set; }

    public Project Project { get; set; } = null!;
}
