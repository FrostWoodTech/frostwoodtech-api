namespace FrostWoodTech.API.Interfaces;

/// <summary>
/// The one seam a live FX rate reaches this API through. Callers depend on this, never on a
/// provider SDK, so swapping the free tier for something with an SLA touches one DI line and
/// nothing else — the same shape as <see cref="IEmailService"/>.
/// </summary>
public interface IExchangeRateProvider
{
    /// <summary>
    /// Every rate the provider knows, keyed by ISO 4217 code, in units per 1 USD. One call covers
    /// every currency — there is no per-currency variant, since a real provider returns the whole
    /// table for one request regardless.
    /// </summary>
    Task<IReadOnlyDictionary<string, decimal>> GetRatesAsync(CancellationToken cancellationToken);
}
