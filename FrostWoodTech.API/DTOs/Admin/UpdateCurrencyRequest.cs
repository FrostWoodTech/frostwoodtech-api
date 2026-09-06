namespace FrostWoodTech.API.DTOs.Admin;

/// <summary>Full replacement — every field is written, so send the whole currency. Only
/// <c>manualRateFromUsd</c> can be written this way; the live rate only changes via a refresh.</summary>
public sealed class UpdateCurrencyRequest
{
    public string? Code { get; set; }

    public string? Name { get; set; }

    public string? Symbol { get; set; }

    public decimal? ManualRateFromUsd { get; set; }

    public bool IsActive { get; set; }
}
