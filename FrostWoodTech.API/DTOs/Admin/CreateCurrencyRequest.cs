namespace FrostWoodTech.API.DTOs.Admin;

public sealed class CreateCurrencyRequest
{
    /// <summary>ISO 4217; upper-cased by the API.</summary>
    public string? Code { get; set; }

    public string? Name { get; set; }

    public string? Symbol { get; set; }

    /// <summary>Units per 1 USD, greater than zero. Null means use the live rate; USD must be exactly 1.</summary>
    public decimal? ManualRateFromUsd { get; set; }

    public bool IsActive { get; set; }
}
