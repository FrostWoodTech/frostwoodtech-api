using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.Auth;

/// <summary>Scoped holder the JWT middleware fills; the isolated worker has no HttpContext.User for services.</summary>
public sealed class CurrentUser
{
    public Guid? UserId { get; set; }

    public string? Email { get; set; }

    public UserRole? Role { get; set; }

    public bool IsAuthenticated => UserId is not null;

    public bool IsSuperAdmin => Role == UserRole.SuperAdmin;
}
