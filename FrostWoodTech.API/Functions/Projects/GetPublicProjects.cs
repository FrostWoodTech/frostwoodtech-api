using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Projects;

public class GetPublicProjects
{
    private readonly IProjectService _projectService;

    public GetPublicProjects(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [Function("GetPublicProjects")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "public/projects")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        if (!QueryParameters.TryReadSite(req, out var site))
        {
            return ProblemResults.BadRequest("validation_failed", "Unknown site.");
        }

        if (site is null)
        {
            return ProblemResults.SiteRequired();
        }

        var tagSlug = QueryParameters.ReadString(req, "tag");

        // A category tag slug; technology tags use ?tag=.
        var categorySlug = QueryParameters.ReadString(req, "category");
        var featured = QueryParameters.ReadBool(req, "featured");
        var (page, pageSize) = QueryParameters.ReadPaging(req);

        var result = await _projectService.GetPublicProjectsAsync(
            site.Value,
            tagSlug,
            categorySlug,
            featured,
            page,
            pageSize,
            cancellationToken);

        return HttpResponses.PublicJson(req, result);
    }
}
