using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Certificates;

public class GetPublicCertificates
{
    private readonly ICertificateService _certificateService;

    public GetPublicCertificates(ICertificateService certificateService)
    {
        _certificateService = certificateService;
    }

    /// <summary>No `?site=` — certificates only ever exist for the personal site.</summary>
    [Function("GetPublicCertificates")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "public/certificates")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        var featured = QueryParameters.ReadBool(req, "featured");
        var (page, pageSize) = QueryParameters.ReadPaging(req);

        var result = await _certificateService.GetPublicCertificatesAsync(featured, page, pageSize, cancellationToken);

        return HttpResponses.PublicJson(req, result);
    }
}
