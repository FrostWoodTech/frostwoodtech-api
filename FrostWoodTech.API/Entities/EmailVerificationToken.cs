namespace FrostWoodTech.API.Entities;

/// <summary>Stores only the hash. Not soft-deletable so replayed links are recognised.</summary>
public class EmailVerificationToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public required string TokenHash { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UsedAt { get; set; }

    public bool IsActive(DateTimeOffset now) => UsedAt is null && ExpiresAt > now;
}
