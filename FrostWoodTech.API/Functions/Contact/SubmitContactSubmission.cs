using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.DTOs.Public;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Contact;

public class SubmitContactSubmission
{
    private readonly IContactService _contactService;

    public SubmitContactSubmission(IContactService contactService)
    {
        _contactService = contactService;
    }

    /// <summary>
    /// Anonymous public write. Never public content — there is no matching public read; this
    /// lands only in the admin inbox.
    /// </summary>
    [Function("SubmitContactSubmission")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "public/contact")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        CreateContactSubmissionRequest? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<CreateContactSubmissionRequest>(
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

        var ipAddress = ClientAddress.Read(req);

        var result = await _contactService.SubmitAsync(body, ipAddress, cancellationToken);
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
