using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Pricing;

public class DeletePricingPlanFeature
{
    private readonly IPricingService _pricing;

    public DeletePricingPlanFeature(IPricingService pricing)
    {
        _pricing = pricing;
    }

    [Function("DeletePricingPlanFeature")]
    public async Task<IActionResult> Run(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "delete",
            Route = "cms/admin/pricing-plans/{id:guid}/features/{featureId:guid}")] HttpRequest req,
        Guid id,
        Guid featureId,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        var result = await _pricing.DeleteFeatureAsync(id, featureId, cancellationToken);

        return result.IsSuccess
            ? new NoContentResult()
            : ProblemResults.FromError(result.Error!);
    }
}
