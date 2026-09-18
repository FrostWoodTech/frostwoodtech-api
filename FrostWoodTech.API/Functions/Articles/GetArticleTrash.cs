using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Articles;

public class GetArticleTrash
{
    private readonly IArticleService _articleService;

    public GetArticleTrash(IArticleService articleService)
    {
        _articleService = articleService;
    }

    [Function("GetArticleTrash")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cms/admin/articles/trash")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        var search = QueryParameters.ReadString(req, "search");
        var (page, pageSize) = QueryParameters.ReadPaging(req);

        var result = await _articleService.GetTrashAsync(search, page, pageSize, cancellationToken);

        return new OkObjectResult(result);
    }
}
