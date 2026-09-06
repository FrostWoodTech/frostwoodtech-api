namespace FrostWoodTech.API.Entities;

/// <summary>
/// Join between a service and the projects shown as its case studies. Named
/// <c>ServiceProject</c> rather than <c>ProjectService</c> so the type does not collide with
/// <see cref="FrostWoodTech.API.Services.ProjectService"/> — the same reason
/// <see cref="ServiceOffering"/> is not called <c>Service</c>.
/// </summary>
public class ServiceProject
{
    public Guid ServiceId { get; set; }

    public ServiceOffering Service { get; set; } = null!;

    public Guid ProjectId { get; set; }

    public Project Project { get; set; } = null!;
}
