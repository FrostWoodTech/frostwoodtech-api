namespace FrostWoodTech.API.DTOs.Admin;

/// <summary>Full replacement. The live rate only changes via a refresh.</summary>
public sealed class UpdateCurrencyRequest
{
    public string? Code { get; set; }

    public string? Name { get; set; }

    public string? Symbol { get; set; }

    public decimal? ManualRateFromUsd { get; set; }

    public bool IsActive { get; set; }
}
