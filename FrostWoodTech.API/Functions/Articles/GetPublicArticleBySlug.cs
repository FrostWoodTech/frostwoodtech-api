using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Articles;

public class GetPublicArticleBySlug
{
    private readonly IArticleService _articleService;

    public GetPublicArticleBySlug(IArticleService articleService)
    {
        _articleService = articleService;
    }

    [Function("GetPublicArticleBySlug")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "public/articles/{slug}")] HttpRequest req,
        string slug,
        CancellationToken cancellationToken)
    {
        var result = await _articleService.GetPublicArticleBySlugAsync(slug, cancellationToken);

        return result.IsSuccess
            ? HttpResponses.PublicJson(req, result.Value!)
            : ProblemResults.FromError(result.Error!);
    }
}
