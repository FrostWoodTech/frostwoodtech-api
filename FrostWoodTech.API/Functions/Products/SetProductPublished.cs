using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Products;

public class SetProductPublished
{
    private readonly IProductService _productService;

    public SetProductPublished(IProductService productService)
    {
        _productService = productService;
    }

    [Function("SetProductPublished")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "cms/admin/products/{id:guid}/publish")] HttpRequest req,
        Guid id,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        SetPublishedRequest? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<SetPublishedRequest>(
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

        var result = await _productService.SetPublishedAsync(id, body, cancellationToken);

        return result.IsSuccess
            ? new OkObjectResult(result.Value)
            : ProblemResults.FromError(result.Error!);
    }
}
