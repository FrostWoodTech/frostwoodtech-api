namespace FrostWoodTech.API.DTOs.Admin;

public class SetPasswordRequest
{
    public string? Token { get; set; }

    public string? Password { get; set; }

    public string? ConfirmPassword { get; set; }
}
