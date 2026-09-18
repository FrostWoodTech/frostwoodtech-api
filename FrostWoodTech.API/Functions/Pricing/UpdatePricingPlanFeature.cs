using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Pricing;

public class UpdatePricingPlanFeature
{
    private readonly IPricingService _pricing;

    public UpdatePricingPlanFeature(IPricingService pricing)
    {
        _pricing = pricing;
    }

    [Function("UpdatePricingPlanFeature")]
    public async Task<IActionResult> Run(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "put",
            Route = "cms/admin/pricing-plans/{id:guid}/features/{featureId:guid}")] HttpRequest req,
        Guid id,
        Guid featureId,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        UpdatePricingPlanFeatureRequest? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<UpdatePricingPlanFeatureRequest>(
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

        var result = await _pricing.UpdateFeatureAsync(id, featureId, body, cancellationToken);

        return result.IsSuccess
            ? new OkObjectResult(result.Value)
            : ProblemResults.FromError(result.Error!);
    }
}
