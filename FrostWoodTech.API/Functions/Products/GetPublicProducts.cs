using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Products;

public class GetPublicProducts
{
    private readonly IProductService _productService;

    public GetPublicProducts(IProductService productService)
    {
        _productService = productService;
    }

    [Function("GetPublicProducts")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "public/products")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        var featured = QueryParameters.ReadBool(req, "featured");
        var (page, pageSize) = QueryParameters.ReadPaging(req);

        var result = await _productService.GetPublicProductsAsync(
            featured,
            page,
            pageSize,
            cancellationToken);

        return HttpResponses.PublicJson(req, result);
    }
}
