using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Auth;

public class GoogleSignIn
{
    private readonly IUserService _userService;

    public GoogleSignIn(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Exchanges a Google ID token for the same JWT pair password login returns. Anonymous by
    /// necessity — this is one of the ways a caller gets a token in the first place.
    /// </summary>
    [Function("GoogleSignIn")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "cms/admin/auth/google")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        GoogleSignInRequest? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<GoogleSignInRequest>(
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

        var result = await _userService.GoogleSignInAsync(body, cancellationToken);

        if (!result.IsSuccess)
            return ProblemResults.FromError(result.Error!);

        HttpResponses.SetRefreshTokenCookie(req, result.Value!.RefreshToken, result.Value.RefreshTokenExpiresAt);
        return new OkObjectResult(result.Value);
    }
}
