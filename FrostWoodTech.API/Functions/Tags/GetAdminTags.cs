using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.Enums;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Tags;

public class GetAdminTags
{
    private readonly ITagService _tagService;

    public GetAdminTags(ITagService tagService)
    {
        _tagService = tagService;
    }

    [Function("GetAdminTags")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cms/admin/tags")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        var isTechnology = QueryParameters.ReadBool(req, "isTechnology");

        if (!QueryParameters.TryReadEnum<TechCategory>(req, "category", out var category))
        {
            return ProblemResults.BadRequest("validation_failed", "Unknown category.");
        }

        var search = QueryParameters.ReadString(req, "search");
        var (page, pageSize) = QueryParameters.ReadPaging(req);

        var result = await _tagService.GetAdminTagsAsync(
            isTechnology,
            category,
            search,
            page,
            pageSize,
            cancellationToken);

        return new OkObjectResult(result);
    }
}
