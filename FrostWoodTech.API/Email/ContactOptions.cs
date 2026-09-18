namespace FrostWoodTech.API.Email;

public sealed class ContactOptions
{
    /// <summary>Where new-submission notifications go. Empty falls back to SuperAdmin:Email.</summary>
    public string NotifyAddress { get; set; } = string.Empty;
}
