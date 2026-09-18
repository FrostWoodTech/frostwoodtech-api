using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.Enums;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Contact;

public class GetAdminContactSubmissions
{
    private readonly IContactService _contactService;

    public GetAdminContactSubmissions(IContactService contactService)
    {
        _contactService = contactService;
    }

    [Function("GetAdminContactSubmissions")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cms/admin/contact-submissions")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        if (!QueryParameters.TryReadEnum<ContactSubmissionStatus>(req, "status", out var status))
        {
            return ProblemResults.BadRequest("validation_failed", "Unknown ?status= value.");
        }

        if (!QueryParameters.TryReadSite(req, out var site))
        {
            return ProblemResults.BadRequest("validation_failed", "Unknown ?site= value.");
        }

        var serviceId = QueryParameters.ReadGuid(req, "serviceId");
        var search = QueryParameters.ReadString(req, "search");
        var (page, pageSize) = QueryParameters.ReadPaging(req);

        var result = await _contactService.GetAdminSubmissionsAsync(
            status,
            site,
            serviceId,
            search,
            page,
            pageSize,
            cancellationToken);

        return new OkObjectResult(result);
    }
}
