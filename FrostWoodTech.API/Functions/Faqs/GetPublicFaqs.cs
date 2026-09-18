using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Faqs;

public class GetPublicFaqs
{
    private readonly IFaqService _faqService;

    public GetPublicFaqs(IFaqService faqService)
    {
        _faqService = faqService;
    }

    [Function("GetPublicFaqs")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "public/faqs")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        if (!QueryParameters.TryReadSite(req, out var site))
        {
            return ProblemResults.BadRequest("validation_failed", "Unknown site.");
        }

        if (site is null)
        {
            return ProblemResults.SiteRequired();
        }

        var result = await _faqService.GetPublicFaqsAsync(site.Value, cancellationToken);

        return HttpResponses.PublicJson(req, result);
    }
}
