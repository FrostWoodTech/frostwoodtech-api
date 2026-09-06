using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Articles;

public class GetAdminArticles
{
    private readonly IArticleService _articleService;

    public GetAdminArticles(IArticleService articleService)
    {
        _articleService = articleService;
    }

    [Function("GetAdminArticles")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cms/admin/articles")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        // Optional here, unlike the public surface: the admin SPA lists across both sites.
        if (!QueryParameters.TryReadSite(req, out var site))
        {
            return ProblemResults.BadRequest("validation_failed", "Unknown site.");
        }

        var isPublished = QueryParameters.ReadBool(req, "isPublished");
        var search = QueryParameters.ReadString(req, "search");
        var includeHidden = QueryParameters.ReadBool(req, "includeHidden") ?? false;
        var (page, pageSize) = QueryParameters.ReadPaging(req);

        var result = await _articleService.GetAdminArticlesAsync(
            site,
            isPublished,
            search,
            includeHidden,
            page,
            pageSize,
            cancellationToken);

        return new OkObjectResult(result);
    }
}
