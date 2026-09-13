using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.Entities;

/// <summary>Rate-limit record for failed logins and reset requests. In Postgres so it works across instances.</summary>
public class LoginAttempt
{
    public Guid Id { get; set; }

    /// <summary>Lowercased; stored even for unknown users.</summary>
    public required string Email { get; set; }

    public string? IpAddress { get; set; }

    public AuthAttemptAction Action { get; set; }

    public DateTimeOffset AttemptedAt { get; set; }
}
