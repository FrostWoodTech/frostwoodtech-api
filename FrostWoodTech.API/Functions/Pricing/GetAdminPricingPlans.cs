using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Pricing;

public class GetAdminPricingPlans
{
    private readonly IPricingService _pricing;

    public GetAdminPricingPlans(IPricingService pricing)
    {
        _pricing = pricing;
    }

    [Function("GetAdminPricingPlans")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cms/admin/pricing-plans")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        var serviceId = QueryParameters.ReadGuid(req, "serviceId");
        var comboOnly = QueryParameters.ReadBool(req, "comboOnly") ?? false;
        var tiersOnly = QueryParameters.ReadBool(req, "tiersOnly") ?? false;
        var isPublished = QueryParameters.ReadBool(req, "isPublished");
        var search = QueryParameters.ReadString(req, "search");
        var (page, pageSize) = QueryParameters.ReadPaging(req);

        var result = await _pricing.GetAdminPlansAsync(
            serviceId,
            comboOnly,
            tiersOnly,
            isPublished,
            search,
            page,
            pageSize,
            cancellationToken);

        return new OkObjectResult(result);
    }
}
