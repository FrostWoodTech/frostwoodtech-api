using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Projects;

public class UpdateProject
{
    private readonly IProjectService _projectService;

    public UpdateProject(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [Function("UpdateProject")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "cms/admin/projects/{id:guid}")] HttpRequest req,
        Guid id,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        UpdateProjectRequest? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<UpdateProjectRequest>(
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

        var result = await _projectService.UpdateAsync(id, body, cancellationToken);

        return result.IsSuccess
            ? new OkObjectResult(result.Value)
            : ProblemResults.FromError(result.Error!);
    }
}
