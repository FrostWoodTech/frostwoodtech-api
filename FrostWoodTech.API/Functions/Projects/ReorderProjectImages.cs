using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Projects;

public class ReorderProjectImages
{
    private readonly IProjectService _projectService;

    public ReorderProjectImages(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [Function("ReorderProjectImages")]
    public async Task<IActionResult> Run(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "post",
            Route = "cms/admin/projects/{id:guid}/images/reorder")] HttpRequest req,
        Guid id,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        ImageReorderRequest? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<ImageReorderRequest>(
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

        var result = await _projectService.ReorderImagesAsync(id, body, cancellationToken);

        return result.IsSuccess
            ? new NoContentResult()
            : ProblemResults.FromError(result.Error!);
    }
}
