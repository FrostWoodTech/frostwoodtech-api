using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Reviews;

public class GetAdminReviews
{
    private readonly IReviewService _reviewService;

    public GetAdminReviews(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [Function("GetAdminReviews")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cms/admin/reviews")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        var isPublished = QueryParameters.ReadBool(req, "isPublished");
        var isFeatured = QueryParameters.ReadBool(req, "isFeatured");
        var country = QueryParameters.ReadString(req, "country");
        var search = QueryParameters.ReadString(req, "search");
        var (page, pageSize) = QueryParameters.ReadPaging(req);

        var result = await _reviewService.GetAdminReviewsAsync(
            isPublished,
            isFeatured,
            country,
            search,
            page,
            pageSize,
            cancellationToken);

        return new OkObjectResult(result);
    }
}
