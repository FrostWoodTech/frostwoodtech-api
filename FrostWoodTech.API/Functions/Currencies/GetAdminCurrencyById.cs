using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Currencies;

public class GetAdminCurrencyById
{
    private readonly ICurrencyService _currencyService;

    public GetAdminCurrencyById(ICurrencyService currencyService)
    {
        _currencyService = currencyService;
    }

    [Function("GetAdminCurrencyById")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cms/admin/currencies/{id:guid}")] HttpRequest req,
        Guid id,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        var result = await _currencyService.GetByIdAsync(id, cancellationToken);

        return result.IsSuccess
            ? new OkObjectResult(result.Value)
            : ProblemResults.FromError(result.Error!);
    }
}
