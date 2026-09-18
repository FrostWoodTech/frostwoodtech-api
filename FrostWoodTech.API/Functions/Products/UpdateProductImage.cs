using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Products;

public class UpdateProductImage
{
    private readonly IProductService _productService;

    public UpdateProductImage(IProductService productService)
    {
        _productService = productService;
    }

    [Function("UpdateProductImage")]
    public async Task<IActionResult> Run(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "put",
            Route = "cms/admin/products/{id:guid}/images/{imageId:guid}")] HttpRequest req,
        Guid id,
        Guid imageId,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        UpdateProductImageRequest? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<UpdateProductImageRequest>(
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

        var result = await _productService.UpdateImageAsync(id, imageId, body, cancellationToken);

        return result.IsSuccess
            ? new OkObjectResult(result.Value)
            : ProblemResults.FromError(result.Error!);
    }
}
