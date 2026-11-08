using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Articles;

public class GetPublicArticles
{
    private readonly IArticleService _articleService;

    public GetPublicArticles(IArticleService articleService)
    {
        _articleService = articleService;
    }

    [Function("GetPublicArticles")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "public/articles")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        var tagSlug = QueryParameters.ReadString(req, "tag");
        var featured = QueryParameters.ReadBool(req, "featured");
        var (page, pageSize) = QueryParameters.ReadPaging(req);

        var result = await _articleService.GetPublicArticlesAsync(
            tagSlug,
            featured,
            page,
            pageSize,
            cancellationToken);

        return HttpResponses.PublicJson(req, result);
    }
}
