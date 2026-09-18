using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Faqs;

public class GetFaqTrash
{
    private readonly IFaqService _faqService;

    public GetFaqTrash(IFaqService faqService)
    {
        _faqService = faqService;
    }

    [Function("GetFaqTrash")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cms/admin/faqs/trash")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        var search = QueryParameters.ReadString(req, "search");
        var (page, pageSize) = QueryParameters.ReadPaging(req);

        var result = await _faqService.GetTrashAsync(search, page, pageSize, cancellationToken);

        return new OkObjectResult(result);
    }
}
