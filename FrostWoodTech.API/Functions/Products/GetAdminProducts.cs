using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Products;

public class GetAdminProducts
{
    private readonly IProductService _productService;

    public GetAdminProducts(IProductService productService)
    {
        _productService = productService;
    }

    [Function("GetAdminProducts")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cms/admin/products")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        // Optional on admin lists, unlike the public surface.
        if (!QueryParameters.TryReadSite(req, out var site))
        {
            return ProblemResults.BadRequest("validation_failed", "Unknown site.");
        }

        var isPublished = QueryParameters.ReadBool(req, "isPublished");
        var search = QueryParameters.ReadString(req, "search");
        var includeHidden = QueryParameters.ReadBool(req, "includeHidden") ?? false;
        var (page, pageSize) = QueryParameters.ReadPaging(req);

        var result = await _productService.GetAdminProductsAsync(
            site,
            isPublished,
            search,
            includeHidden,
            page,
            pageSize,
            cancellationToken);

        return new OkObjectResult(result);
    }
}
