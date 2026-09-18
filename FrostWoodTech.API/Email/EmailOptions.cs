namespace FrostWoodTech.API.Email;

public sealed class EmailOptions
{
    /// <summary>"brevo" sends for real; anything else (default "log") only logs.</summary>
    public string Provider { get; set; } = "log";

    public string FromAddress { get; set; } = string.Empty;

    public string FromName { get; set; } = "FrostWoodTech";

    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Admin SPA base URL, used to build verification and password links.</summary>
    public string BaseUrl { get; set; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(FromAddress);
}
