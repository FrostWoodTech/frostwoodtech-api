namespace FrostWoodTech.API.DTOs.Admin;

public sealed class RejectUserRequest
{
    /// <summary>Required: shown to the user at sign-in.</summary>
    public string? Reason { get; set; }
}
