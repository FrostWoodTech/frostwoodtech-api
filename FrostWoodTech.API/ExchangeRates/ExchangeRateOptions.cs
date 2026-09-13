namespace FrostWoodTech.API.ExchangeRates;

public sealed class ExchangeRateOptions
{
    /// <summary>Free, keyless provider; override only to switch provider.</summary>
    public string BaseUrl { get; set; } = "https://open.er-api.com/v6/latest/USD";
}
