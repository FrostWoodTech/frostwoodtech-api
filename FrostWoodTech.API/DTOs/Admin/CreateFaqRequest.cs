namespace FrostWoodTech.API.DTOs.Admin;

public class CreateFaqRequest
{
    /// <summary>Null means a general FAQ shared by both sites; otherwise scopes it to one service's page.</summary>
    public Guid? ServiceId { get; set; }

    public string? Question { get; set; }

    /// <summary>Markdown, stored raw.</summary>
    public string? Answer { get; set; }

    public bool IsPublished { get; set; }

    public bool ShowOnAgency { get; set; }

    public bool ShowOnPersonal { get; set; }
}
