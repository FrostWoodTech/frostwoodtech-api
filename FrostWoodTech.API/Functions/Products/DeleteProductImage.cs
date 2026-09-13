using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Products;

public class DeleteProductImage
{
    private readonly IProductService _productService;

    public DeleteProductImage(IProductService productService)
    {
        _productService = productService;
    }

    [Function("DeleteProductImage")]
    public async Task<IActionResult> Run(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "delete",
            Route = "cms/admin/products/{id:guid}/images/{imageId:guid}")] HttpRequest req,
        Guid id,
        Guid imageId,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        var result = await _productService.DeleteImageAsync(id, imageId, cancellationToken);

        return result.IsSuccess
            ? new NoContentResult()
            : ProblemResults.FromError(result.Error!);
    }
}
