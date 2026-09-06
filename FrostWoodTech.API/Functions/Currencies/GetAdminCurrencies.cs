using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Currencies;

public class GetAdminCurrencies
{
    private readonly ICurrencyService _currencyService;

    public GetAdminCurrencies(ICurrencyService currencyService)
    {
        _currencyService = currencyService;
    }

    [Function("GetAdminCurrencies")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cms/admin/currencies")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        var isActive = QueryParameters.ReadBool(req, "isActive");
        var search = QueryParameters.ReadString(req, "search");
        var (page, pageSize) = QueryParameters.ReadPaging(req);

        var result = await _currencyService.GetAdminCurrenciesAsync(
            isActive,
            search,
            page,
            pageSize,
            cancellationToken);

        return new OkObjectResult(result);
    }
}
