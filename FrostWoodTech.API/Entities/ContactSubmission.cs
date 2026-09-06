using FrostWoodTech.API.Entities.Common;
using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.Entities;

/// <summary>
/// A visitor's "contact us" enquiry. Never public content — there is no public read path and no
/// is_published flag; it exists only to be triaged in the admin inbox.
/// </summary>
public class ContactSubmission : AuditableEntity
{
    public required string Name { get; set; }

    public required string Email { get; set; }

    public string? Phone { get; set; }

    public string? Company { get; set; }

    public string? Subject { get; set; }

    public required string Message { get; set; }

    /// <summary>The service the enquiry is about. Null means a general enquiry.</summary>
    public Guid? ServiceId { get; set; }

    public ServiceOffering? Service { get; set; }

    public ContactBudgetRange? BudgetRange { get; set; }

    /// <summary>Which public frontend the form was submitted from.</summary>
    public Site Site { get; set; }

    public ContactSubmissionStatus Status { get; set; }

    /// <summary>Internal triage notes. Admin-only — there is no public DTO carrying this.</summary>
    public string? AdminNotes { get; set; }

    public DateTimeOffset? RepliedAt { get; set; }

    public Guid? RepliedBy { get; set; }

    public User? RepliedByUser { get; set; }

    /// <summary>Admin-only, for spam moderation and the submission rate limit.</summary>
    public string? SubmitterIp { get; set; }
}
