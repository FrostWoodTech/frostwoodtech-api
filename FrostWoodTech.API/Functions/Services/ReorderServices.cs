using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Services;

public class ReorderServices
{
    private readonly IServiceCatalogService _serviceCatalog;

    public ReorderServices(IServiceCatalogService serviceCatalog)
    {
        _serviceCatalog = serviceCatalog;
    }

    [Function("ReorderServices")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "cms/admin/services/reorder")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        ReorderRequest? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<ReorderRequest>(
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

        var result = await _serviceCatalog.ReorderAsync(body, cancellationToken);

        return result.IsSuccess
            ? new NoContentResult()
            : ProblemResults.FromError(result.Error!);
    }
}
