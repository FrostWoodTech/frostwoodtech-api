using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Faqs;

public class GetAdminFaqs
{
    private readonly IFaqService _faqService;

    public GetAdminFaqs(IFaqService faqService)
    {
        _faqService = faqService;
    }

    [Function("GetAdminFaqs")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cms/admin/faqs")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        // Site is an optional filter here — the admin list defaults to everything.
        if (!QueryParameters.TryReadSite(req, out var site))
        {
            return ProblemResults.BadRequest("validation_failed", "Unknown site.");
        }

        var isPublished = QueryParameters.ReadBool(req, "isPublished");
        var search = QueryParameters.ReadString(req, "search");
        var serviceId = QueryParameters.ReadGuid(req, "serviceId");
        var globalOnly = QueryParameters.ReadBool(req, "globalOnly") ?? false;
        var (page, pageSize) = QueryParameters.ReadPaging(req);

        var result = await _faqService.GetAdminFaqsAsync(
            site,
            isPublished,
            search,
            serviceId,
            globalOnly,
            page,
            pageSize,
            cancellationToken);

        return new OkObjectResult(result);
    }
}
