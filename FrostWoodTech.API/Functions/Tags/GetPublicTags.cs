using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.Enums;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Tags;

public class GetPublicTags
{
    private readonly ITagService _tagService;

    public GetPublicTags(ITagService tagService)
    {
        _tagService = tagService;
    }

    [Function("GetPublicTags")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "public/tags")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        // No ?site=: tags are shared by both sites.
        var isTechnology = QueryParameters.ReadBool(req, "isTechnology");

        if (!QueryParameters.TryReadEnum<TechCategory>(req, "category", out var category))
        {
            return ProblemResults.BadRequest("validation_failed", "Unknown category.");
        }

        var tags = await _tagService.GetPublicTagsAsync(isTechnology, category, cancellationToken);

        return HttpResponses.PublicJson(req, tags);
    }
}
