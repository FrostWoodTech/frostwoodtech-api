using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Auth;

public class ResendVerification
{
    private readonly IUserService _users;

    public ResendVerification(IUserService users)
    {
        _users = users;
    }

    [Function("ResendVerification")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "cms/admin/auth/resend-verification")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        ResendVerificationRequest? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<ResendVerificationRequest>(
                req.Body, JsonDefaults.Options, cancellationToken);
        }
        catch (JsonException ex)
        {
            return ProblemResults.BadRequest("validation_failed", ex.Message);
        }

        if (body is null)
            return ProblemResults.BadRequest("validation_failed", "A request body is required.");

        var result = await _users.ResendVerificationAsync(body, cancellationToken);

        return new ObjectResult(result.Value) { StatusCode = StatusCodes.Status202Accepted };
    }
}
