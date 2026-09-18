using FrostWoodTech.API.Entities.Common;
using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.Entities;

public class User : AuditableEntity
{
    /// <summary>citext, so lookups are case-insensitive.</summary>
    public required string Email { get; set; }

    public required string FirstName { get; set; }

    public required string LastName { get; set; }

    /// <summary>Null for Google-only accounts and the unredeemed super admin.</summary>
    public string? PasswordHash { get; set; }

    public string? GoogleSubjectId { get; set; }

    public string? AvatarUrl { get; set; }

    public UserRole Role { get; set; }

    public UserStatus Status { get; set; }

    public Guid? ApprovedBy { get; set; }

    public User? ApprovedByUser { get; set; }

    public DateTimeOffset? ApprovedAt { get; set; }

    public string? RejectionReason { get; set; }

    public DateTimeOffset? LastLoginAt { get; set; }

    /// <summary>Kept independent of Status so it survives a later reject or disable.</summary>
    public DateTimeOffset? EmailVerifiedAt { get; set; }
}
