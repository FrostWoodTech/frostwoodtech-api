namespace FrostWoodTech.API.Enums;

/// <summary>Spam is set by the server (honeypot), never by the submitter.</summary>
public enum ContactSubmissionStatus
{
    New,
    Read,
    Replied,
    Archived,
    Spam
}
