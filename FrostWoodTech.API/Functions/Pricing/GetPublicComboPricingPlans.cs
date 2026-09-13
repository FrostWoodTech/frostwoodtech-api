using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Pricing;

/// <summary>Own route rather than an omitted serviceId, so the query is visible in the URL.</summary>
public class GetPublicComboPricingPlans
{
    private readonly IPricingService _pricing;

    public GetPublicComboPricingPlans(IPricingService pricing)
    {
        _pricing = pricing;
    }

    [Function("GetPublicComboPricingPlans")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "public/pricing/combos")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        var featured = QueryParameters.ReadBool(req, "featured");
        var (page, pageSize) = QueryParameters.ReadPaging(req);

        var result = await _pricing.GetPublicComboPlansAsync(
            featured,
            page,
            pageSize,
            cancellationToken);

        return HttpResponses.PublicJson(req, result);
    }
}
