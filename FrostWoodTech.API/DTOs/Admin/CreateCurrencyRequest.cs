namespace FrostWoodTech.API.DTOs.Admin;

public sealed class CreateCurrencyRequest
{
    /// <summary>ISO 4217 — 3 letters, upper-cased by the API.</summary>
    public string? Code { get; set; }

    public string? Name { get; set; }

    public string? Symbol { get; set; }

    /// <summary>
    /// "Price set by me." Units per 1 USD; must be greater than zero when present, and exactly 1
    /// for USD itself. Null (the default) means "use the live rate" for every currency but USD,
    /// which always requires a value.
    /// </summary>
    public decimal? ManualRateFromUsd { get; set; }

    public bool IsActive { get; set; }
}
