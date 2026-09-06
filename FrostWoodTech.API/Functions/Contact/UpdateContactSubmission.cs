using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Contact;

public class UpdateContactSubmission
{
    private readonly IContactService _contactService;

    public UpdateContactSubmission(IContactService contactService)
    {
        _contactService = contactService;
    }

    /// <summary>Triage — status and internal notes. Publishing/reordering concepts don't apply here.</summary>
    [Function("UpdateContactSubmission")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "cms/admin/contact-submissions/{id:guid}")] HttpRequest req,
        Guid id,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        UpdateContactSubmissionRequest? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<UpdateContactSubmissionRequest>(
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

        var result = await _contactService.UpdateAsync(id, body, cancellationToken);

        return result.IsSuccess
            ? new OkObjectResult(result.Value)
            : ProblemResults.FromError(result.Error!);
    }
}
