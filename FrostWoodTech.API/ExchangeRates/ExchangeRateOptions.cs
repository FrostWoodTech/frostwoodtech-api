namespace FrostWoodTech.API.ExchangeRates;

/// <summary>Bound from the <c>ExchangeRate</c> configuration section.</summary>
public sealed class ExchangeRateOptions
{
    /// <summary>
    /// A free, keyless provider covering ~160 ISO 4217 codes. No signup and nothing to store as a
    /// secret — appropriate for a rate that's a display convenience, not a financial transaction.
    /// Override only to point at a different provider (e.g. a paid one with an SLA) later.
    /// </summary>
    public string BaseUrl { get; set; } = "https://open.er-api.com/v6/latest/USD";
}
