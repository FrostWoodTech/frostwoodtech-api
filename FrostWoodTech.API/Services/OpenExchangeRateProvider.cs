using System.Net.Http.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Logging;

using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Services;

/// <summary>
/// Reads rates from open.er-api.com's free, keyless endpoint. The <c>HttpClient</c>'s
/// <c>BaseAddress</c> is set to <see cref="ExchangeRates.ExchangeRateOptions.BaseUrl"/> in
/// <c>Program.cs</c>, so a request here is just the trailing path.
/// </summary>
public class OpenExchangeRateProvider : IExchangeRateProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenExchangeRateProvider> _logger;

    public OpenExchangeRateProvider(HttpClient httpClient, ILogger<OpenExchangeRateProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyDictionary<string, decimal>> GetRatesAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<OpenExchangeRateResponse>(
                string.Empty, cancellationToken);

            if (response?.Result != "success" || response.Rates is null)
            {
                _logger.LogWarning(
                    "The exchange rate provider returned no usable rates. Result: {Result}",
                    response?.Result ?? "(no response)");

                return new Dictionary<string, decimal>();
            }

            return response.Rates;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            // A refresh that can't reach the provider leaves every currency's live rate exactly
            // where it was — the caller decides how to report that, this just returns "nothing new."
            _logger.LogError(ex, "Fetching exchange rates failed.");

            return new Dictionary<string, decimal>();
        }
    }

    private sealed class OpenExchangeRateResponse
    {
        [JsonPropertyName("result")]
        public string? Result { get; init; }

        [JsonPropertyName("rates")]
        public Dictionary<string, decimal>? Rates { get; init; }
    }
}
