using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Pricing;

public class ReorderPricingPlans
{
    private readonly IPricingService _pricing;

    public ReorderPricingPlans(IPricingService pricing)
    {
        _pricing = pricing;
    }

    /// <summary>Bulk sort_order update. Pricing is agency-only, so there is one order to keep.</summary>
    [Function("ReorderPricingPlans")]
    public async Task<IActionResult> Run(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "post",
            Route = "cms/admin/pricing-plans/reorder")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        PricingReorderRequest? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<PricingReorderRequest>(
                req.Body,
                JsonDefaults.Options,
                cancellationToken);
        }
        catch (JsonException ex)
        {
            return ProblemResults.BadRequest("validation_failed", ex.Message);
        }

        if (body is null)
        {
            return ProblemResults.BadRequest("validation_failed", "A request body is required.");
        }

        var result = await _pricing.ReorderAsync(body, cancellationToken);

        return result.IsSuccess
            ? new NoContentResult()
            : ProblemResults.FromError(result.Error!);
    }
}
