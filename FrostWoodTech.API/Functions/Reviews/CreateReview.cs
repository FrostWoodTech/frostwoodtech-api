using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Reviews;

public class CreateReview
{
    private readonly IReviewService _reviewService;

    public CreateReview(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [Function("CreateReview")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "cms/admin/reviews")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        CreateReviewRequest? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<CreateReviewRequest>(
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

        var result = await _reviewService.CreateAsync(body, cancellationToken);
        if (!result.IsSuccess)
        {
            return ProblemResults.FromError(result.Error!);
        }

        return new ObjectResult(result.Value)
        {
            StatusCode = StatusCodes.Status201Created
        };
    }
}
