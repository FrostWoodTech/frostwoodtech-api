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

    [Function("GetPublicCurrencies")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "public/currencies")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        // No ?site=: currencies are shared by both sites.
        var currencies = await _currencyService.GetPublicCurrenciesAsync(cancellationToken);

        return HttpResponses.PublicJson(req, currencies);
    }
}
