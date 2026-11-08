using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Services;

public class GetPublicServiceBySlug
{
    private readonly IServiceCatalogService _serviceCatalog;

    public GetPublicServiceBySlug(IServiceCatalogService serviceCatalog)
    {
        _serviceCatalog = serviceCatalog;
    }

    [Function("GetPublicServiceBySlug")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "public/services/{slug}")] HttpRequest req,
        string slug,
        CancellationToken cancellationToken)
    {
        var result = await _serviceCatalog.GetPublicServiceBySlugAsync(slug, cancellationToken);

        return result.IsSuccess
            ? HttpResponses.PublicJson(req, result.Value!)
            : ProblemResults.FromError(result.Error!);
    }
}
