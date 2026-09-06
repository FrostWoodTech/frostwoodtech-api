using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Pricing;

/// <summary>A service page's tiers — Starter, Growth, Pro.</summary>
public class GetPublicServicePricingPlans
{
    private readonly IPricingService _pricing;

    public GetPublicServicePricingPlans(IPricingService pricing)
    {
        _pricing = pricing;
    }

    [Function("GetPublicServicePricingPlans")]
    public async Task<IActionResult> Run(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "get",
            Route = "public/pricing/services/{serviceId:guid}")] HttpRequest req,
        Guid serviceId,
        CancellationToken cancellationToken)
    {
        var (page, pageSize) = QueryParameters.ReadPaging(req);

        var result = await _pricing.GetPublicPlansForServiceAsync(
            serviceId,
            page,
            pageSize,
            cancellationToken);

        return HttpResponses.PublicJson(req, result);
    }
}
