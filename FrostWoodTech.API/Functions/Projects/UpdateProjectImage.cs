using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Projects;

public class UpdateProjectImage
{
    private readonly IProjectService _projectService;

    public UpdateProjectImage(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [Function("UpdateProjectImage")]
    public async Task<IActionResult> Run(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "put",
            Route = "cms/admin/projects/{id:guid}/images/{imageId:guid}")] HttpRequest req,
        Guid id,
        Guid imageId,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        UpdateProjectImageRequest? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<UpdateProjectImageRequest>(
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

        var result = await _projectService.UpdateImageAsync(id, imageId, body, cancellationToken);

        return result.IsSuccess
            ? new OkObjectResult(result.Value)
            : ProblemResults.FromError(result.Error!);
    }
}
