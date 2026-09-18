using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.DTOs.Public;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Reviews;

public class SubmitReview
{
    private readonly IReviewService _reviewService;

    public SubmitReview(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [Function("SubmitReview")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "public/reviews")] HttpRequest req,
        CancellationToken cancellationToken)
    {
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

        var ipAddress = ClientAddress.Read(req);

        var result = await _reviewService.SubmitAsync(body, ipAddress, cancellationToken);
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
