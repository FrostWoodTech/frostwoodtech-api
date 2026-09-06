namespace FrostWoodTech.API.Enums;

/// <summary>
/// Where an enquiry sits in the admin inbox. <c>Spam</c> is set by the server (honeypot), never
/// chosen by the submitter.
/// </summary>
public enum ContactSubmissionStatus
{
    New,
    Read,
    Replied,
    Archived,
    Spam
}
