using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Products;

public class DeleteProduct
{
    private readonly IProductService _productService;

    public DeleteProduct(IProductService productService)
    {
        _productService = productService;
    }

    [Function("DeleteProduct")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "cms/admin/products/{id:guid}")] HttpRequest req,
        Guid id,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        var result = await _productService.DeleteAsync(id, cancellationToken);

        return result.IsSuccess
            ? new NoContentResult()
            : ProblemResults.FromError(result.Error!);
    }
}
