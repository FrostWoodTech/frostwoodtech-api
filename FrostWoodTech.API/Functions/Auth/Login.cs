using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Auth;

public class Login
{
    private readonly IUserService _users;

    public Login(IUserService users)
    {
        _users = users;
    }

    [Function("Login")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "cms/admin/auth/login")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        LoginRequest? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<LoginRequest>(
                req.Body, JsonDefaults.Options, cancellationToken);
        }
        catch (JsonException ex)
        {
            return ProblemResults.BadRequest("validation_failed", ex.Message);
        }

        if (body is null)
            return ProblemResults.BadRequest("validation_failed", "A request body is required.");

        var result = await _users.LoginAsync(body, ClientAddress.Read(req), cancellationToken);

        if (!result.IsSuccess)
            return ProblemResults.FromError(result.Error!);

        HttpResponses.SetRefreshTokenCookie(req, result.Value!.RefreshToken, result.Value.RefreshTokenExpiresAt);
        return new OkObjectResult(result.Value);
    }
}
