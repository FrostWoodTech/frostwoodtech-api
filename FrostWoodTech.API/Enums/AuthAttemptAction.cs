namespace FrostWoodTech.API.Enums;

/// <summary>Login failures and reset requests are rate-limited separately.</summary>
public enum AuthAttemptAction
{
    Login,

    /// <summary>Every request counts, not just failures.</summary>
    PasswordReset
}
