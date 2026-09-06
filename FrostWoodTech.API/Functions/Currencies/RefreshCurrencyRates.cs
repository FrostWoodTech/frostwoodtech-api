using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Currencies;

public class RefreshCurrencyRates
{
    private readonly ICurrencyService _currencyService;

    public RefreshCurrencyRates(ICurrencyService currencyService)
    {
        _currencyService = currencyService;
    }

    /// <summary>The "Refresh now" button — one call to the FX provider, applied to every currency at once.</summary>
    [Function("RefreshCurrencyRates")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "cms/admin/currencies/refresh-rates")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        var result = await _currencyService.RefreshLiveRatesAsync(cancellationToken);

        return result.IsSuccess
            ? new OkObjectResult(result.Value)
            : ProblemResults.FromError(result.Error!);
    }
}
