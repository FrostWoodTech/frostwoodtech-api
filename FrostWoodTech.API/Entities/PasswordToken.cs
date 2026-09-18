using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.Entities;

/// <summary>Setup or reset link. Stores only the hash; not soft-deletable so replays are recognised.</summary>
public class PasswordToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public required string TokenHash { get; set; }

    public PasswordTokenPurpose Purpose { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UsedAt { get; set; }

    public bool IsActive(DateTimeOffset now) => UsedAt is null && ExpiresAt > now;
}
