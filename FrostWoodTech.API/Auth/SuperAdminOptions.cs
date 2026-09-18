namespace FrostWoodTech.API.Auth;

/// <summary>Identity only: no password lives in configuration.</summary>
public sealed class SuperAdminOptions
{
    public string Email { get; set; } = string.Empty;

    public string FirstName { get; set; } = "Super";

    public string LastName { get; set; } = "Admin";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Email);
}
