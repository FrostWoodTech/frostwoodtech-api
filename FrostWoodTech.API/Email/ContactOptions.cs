namespace FrostWoodTech.API.Email;

/// <summary>
/// Bound from the <c>Contact</c> configuration section. Where a new contact-form submission's
/// notification email is sent.
/// </summary>
public sealed class ContactOptions
{
    /// <summary>
    /// Where new-submission notifications go. Empty falls back to <c>SuperAdmin:Email</c> — see
    /// <see cref="Services.ContactService"/> — rather than emailing every approved admin, since a
    /// growing admin roster should not silently widen who gets these.
    /// </summary>
    public string NotifyAddress { get; set; } = string.Empty;
}
