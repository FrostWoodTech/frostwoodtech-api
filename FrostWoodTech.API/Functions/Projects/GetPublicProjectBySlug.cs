using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Projects;

public class GetPublicProjectBySlug
{
    private readonly IProjectService _projectService;

    public GetPublicProjectBySlug(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [Function("GetPublicProjectBySlug")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "public/projects/{slug}")] HttpRequest req,
        string slug,
        CancellationToken cancellationToken)
    {
        var result = await _projectService.GetPublicProjectBySlugAsync(slug, cancellationToken);

        return result.IsSuccess
            ? HttpResponses.PublicJson(req, result.Value!)
            : ProblemResults.FromError(result.Error!);
    }
}
