using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Pricing;

public class GetPricingPlanTrash
{
    private readonly IPricingService _pricingService;

    public GetPricingPlanTrash(IPricingService pricingService)
    {
        _pricingService = pricingService;
    }

    [Function("GetPricingPlanTrash")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cms/admin/pricing-plans/trash")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        var search = QueryParameters.ReadString(req, "search");
        var (page, pageSize) = QueryParameters.ReadPaging(req);

        var result = await _pricingService.GetTrashAsync(search, page, pageSize, cancellationToken);

        return new OkObjectResult(result);
    }
}
