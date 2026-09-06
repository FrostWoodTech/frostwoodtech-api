using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Certificates;

public class GetAdminCertificates
{
    private readonly ICertificateService _certificateService;

    public GetAdminCertificates(ICertificateService certificateService)
    {
        _certificateService = certificateService;
    }

    [Function("GetAdminCertificates")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cms/admin/certificates")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        var isPublished = QueryParameters.ReadBool(req, "isPublished");
        var search = QueryParameters.ReadString(req, "search");
        var (page, pageSize) = QueryParameters.ReadPaging(req);

        var result = await _certificateService.GetAdminCertificatesAsync(
            isPublished,
            search,
            page,
            pageSize,
            cancellationToken);

        return new OkObjectResult(result);
    }
}
