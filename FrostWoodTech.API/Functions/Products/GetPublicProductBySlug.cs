using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Products;

public class GetPublicProductBySlug
{
    private readonly IProductService _productService;

    public GetPublicProductBySlug(IProductService productService)
    {
        _productService = productService;
    }

    [Function("GetPublicProductBySlug")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "public/products/{slug}")] HttpRequest req,
        string slug,
        CancellationToken cancellationToken)
    {
        var result = await _productService.GetPublicProductBySlugAsync(slug, cancellationToken);

        return result.IsSuccess
            ? HttpResponses.PublicJson(req, result.Value!)
            : ProblemResults.FromError(result.Error!);
    }
}
