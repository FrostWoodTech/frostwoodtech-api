using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Auth;

public class ForgotPassword
{
    private readonly IUserService _users;

    public ForgotPassword(IUserService users)
    {
        _users = users;
    }

    /// <summary>No failure branch: the service always succeeds so the response reveals nothing.</summary>
    [Function("ForgotPassword")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "cms/admin/auth/forgot-password")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        ForgotPasswordRequest? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<ForgotPasswordRequest>(
                req.Body, JsonDefaults.Options, cancellationToken);
        }
        catch (JsonException ex)
        {
            return ProblemResults.BadRequest("validation_failed", ex.Message);
        }

        if (body is null)
            return ProblemResults.BadRequest("validation_failed", "A request body is required.");

        var result = await _users.ForgotPasswordAsync(body, ClientAddress.Read(req), cancellationToken);

        return new OkObjectResult(result.Value);
    }
}
