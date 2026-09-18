namespace FrostWoodTech.API.Interfaces;

public interface IExchangeRateProvider
{
    /// <summary>All known rates in units per 1 USD, keyed by ISO code. Empty means the provider was unreachable.</summary>
    Task<IReadOnlyDictionary<string, decimal>> GetRatesAsync(CancellationToken cancellationToken);
}
