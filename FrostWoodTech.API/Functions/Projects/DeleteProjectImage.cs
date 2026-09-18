using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Projects;

public class DeleteProjectImage
{
    private readonly IProjectService _projectService;

    public DeleteProjectImage(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [Function("DeleteProjectImage")]
    public async Task<IActionResult> Run(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "delete",
            Route = "cms/admin/projects/{id:guid}/images/{imageId:guid}")] HttpRequest req,
        Guid id,
        Guid imageId,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        var result = await _projectService.DeleteImageAsync(id, imageId, cancellationToken);

        return result.IsSuccess
            ? new NoContentResult()
            : ProblemResults.FromError(result.Error!);
    }
}
