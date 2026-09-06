using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Currencies;

public class GetPublicCurrencies
{
    private readonly ICurrencyService _currencyService;

    public GetPublicCurrencies(ICurrencyService currencyService)
    {
        _currencyService = currencyService;
    }

    /// <summary>The currencies a visitor may switch to, with the rate to convert at.</summary>
    [Function("GetPublicCurrencies")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "public/currencies")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        // No ?site= here: currencies have no site visibility, they are a shared lookup set.
        var currencies = await _currencyService.GetPublicCurrenciesAsync(cancellationToken);

        return HttpResponses.PublicJson(req, currencies);
    }
}
