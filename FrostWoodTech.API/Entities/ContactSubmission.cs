using FrostWoodTech.API.Entities.Common;
using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.Entities;

/// <summary>Contact enquiry for the admin inbox; never public.</summary>
public class ContactSubmission : AuditableEntity
{
    public required string Name { get; set; }

    public required string Email { get; set; }

    public string? Phone { get; set; }

    public string? Company { get; set; }

    public string? Subject { get; set; }

    public required string Message { get; set; }

    public Guid? ServiceId { get; set; }

    public ServiceOffering? Service { get; set; }

    public ContactBudgetRange? BudgetRange { get; set; }

    public Site Site { get; set; }

    public ContactSubmissionStatus Status { get; set; }

    public string? AdminNotes { get; set; }

    public DateTimeOffset? RepliedAt { get; set; }

    public Guid? RepliedBy { get; set; }

    public User? RepliedByUser { get; set; }

    public string? SubmitterIp { get; set; }
}
