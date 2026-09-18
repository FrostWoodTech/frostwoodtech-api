using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.DTOs.Public;

public class CreateContactSubmissionRequest
{
    public string? Name { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Company { get; set; }

    public string? Subject { get; set; }

    public string? Message { get; set; }

    /// <summary>Null means a general enquiry.</summary>
    public Guid? ServiceId { get; set; }

    public ContactBudgetRange? BudgetRange { get; set; }

    public Site? Site { get; set; }

    /// <summary>Honeypot: any value files the submission as spam, still answered 201.</summary>
    public string? Website { get; set; }
}
