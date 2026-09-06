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

    /// <summary>
    /// Anonymous by design — this has to work precisely because the access token has expired. See
    /// the allow-list in <c>JwtAuthenticationMiddleware</c>. The refresh token itself travels as
    /// an httpOnly cookie, not a body field — there is nothing for JS to read or forward.
    /// </summary>
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
