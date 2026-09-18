namespace FrostWoodTech.API.Email;

public sealed class EmailSendResult
{
    /// <summary>Provider message id, when the transport returns one.</summary>
    public string? MessageId { get; set; }

    public string Provider { get; set; } = string.Empty;
}
