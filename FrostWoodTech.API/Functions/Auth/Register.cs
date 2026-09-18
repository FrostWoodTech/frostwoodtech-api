using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Auth;

public class Register
{
    private readonly IUserService _users;

    public Register(IUserService users)
    {
        _users = users;
    }

    [Function("Register")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "cms/admin/auth/register")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        RegisterRequest? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<RegisterRequest>(
                req.Body, JsonDefaults.Options, cancellationToken);
        }
        catch (JsonException ex)
        {
            return ProblemResults.BadRequest("validation_failed", ex.Message);
        }

        if (body is null)
            return ProblemResults.BadRequest("validation_failed", "A request body is required.");

        var result = await _users.RegisterAsync(body, cancellationToken);
        if (!result.IsSuccess)
            return ProblemResults.FromError(result.Error!);

        return new ObjectResult(result.Value) { StatusCode = StatusCodes.Status201Created };
    }
}
