using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Auth;

public class Refresh
{
    private readonly IUserService _users;

    public Refresh(IUserService users)
    {
        _users = users;
    }

    /// <summary>Must work with an expired access token. The refresh token travels only as an httpOnly cookie.</summary>
    [Function("Refresh")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "cms/admin/auth/refresh")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        var body = new RefreshTokenRequest { RefreshToken = HttpResponses.ReadRefreshTokenCookie(req) };

        var result = await _users.RefreshAsync(body, cancellationToken);

        if (!result.IsSuccess)
            return ProblemResults.FromError(result.Error!);

        HttpResponses.SetRefreshTokenCookie(req, result.Value!.RefreshToken, result.Value.RefreshTokenExpiresAt);
        return new OkObjectResult(result.Value);
    }
}
