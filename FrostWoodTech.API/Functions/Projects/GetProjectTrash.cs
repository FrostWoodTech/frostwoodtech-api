using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Projects;

public class GetProjectTrash
{
    private readonly IProjectService _projectService;

    public GetProjectTrash(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [Function("GetProjectTrash")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cms/admin/projects/trash")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        var search = QueryParameters.ReadString(req, "search");
        var (page, pageSize) = QueryParameters.ReadPaging(req);

        var result = await _projectService.GetTrashAsync(search, page, pageSize, cancellationToken);

        return new OkObjectResult(result);
    }
}
