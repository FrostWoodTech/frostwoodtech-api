using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Reviews;

public class GetReviewTrash
{
    private readonly IReviewService _reviewService;

    public GetReviewTrash(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [Function("GetReviewTrash")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cms/admin/reviews/trash")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        var search = QueryParameters.ReadString(req, "search");
        var (page, pageSize) = QueryParameters.ReadPaging(req);

        var result = await _reviewService.GetTrashAsync(search, page, pageSize, cancellationToken);

        return new OkObjectResult(result);
    }
}
