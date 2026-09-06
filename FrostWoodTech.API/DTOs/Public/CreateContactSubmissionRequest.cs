using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.DTOs.Public;

/// <summary>
/// What an anonymous visitor submits through the contact form. Status, reply tracking and the
/// submitter IP are all server-side — nothing here can set them.
/// </summary>
public class CreateContactSubmissionRequest
{
    public string? Name { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Company { get; set; }

    public string? Subject { get; set; }

    public string? Message { get; set; }

    /// <summary>The service this is about. Null (or absent) means a general enquiry.</summary>
    public Guid? ServiceId { get; set; }

    public ContactBudgetRange? BudgetRange { get; set; }

    /// <summary>Which frontend the form was on. Required — <c>agency</c> or <c>personal</c>.</summary>
    public Site? Site { get; set; }

    /// <summary>
    /// Honeypot. The real form renders this hidden and leaves it empty; a bot fills every field it
    /// finds. Anything non-empty is filed as spam and still answered 200, so the bot learns nothing.
    /// </summary>
    public string? Website { get; set; }
}
