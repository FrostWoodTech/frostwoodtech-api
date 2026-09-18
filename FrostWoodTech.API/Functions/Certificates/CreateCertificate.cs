using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Certificates;

public class CreateCertificate
{
    private readonly ICertificateService _certificateService;

    public CreateCertificate(ICertificateService certificateService)
    {
        _certificateService = certificateService;
    }

    [Function("CreateCertificate")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "cms/admin/certificates")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        CreateCertificateRequest? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<CreateCertificateRequest>(
                req.Body,
                JsonDefaults.Options,
                cancellationToken);
        }
        catch (JsonException ex)
        {
            return ProblemResults.BadRequest("validation_failed", ex.Message);
        }

        if (body is null)
        {
            return ProblemResults.BadRequest("validation_failed", "A request body is required.");
        }

        var result = await _certificateService.CreateAsync(body, cancellationToken);
        if (!result.IsSuccess)
        {
            return ProblemResults.FromError(result.Error!);
        }

        return new ObjectResult(result.Value)
        {
            StatusCode = StatusCodes.Status201Created
        };
    }
}
