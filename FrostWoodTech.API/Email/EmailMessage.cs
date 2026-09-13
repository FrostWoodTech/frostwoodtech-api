namespace FrostWoodTech.API.Email;

/// <summary>No From field: the sender always comes from EmailOptions, so callers can't spoof it.</summary>
public sealed class EmailMessage
{
    public string To { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string HtmlBody { get; set; } = string.Empty;

    public string? TextBody { get; set; }

    public string? ReplyTo { get; set; }
}
