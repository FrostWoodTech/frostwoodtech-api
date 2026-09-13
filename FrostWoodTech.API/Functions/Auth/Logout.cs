using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Auth;

public class Logout
{
    private readonly IUserService _users;

    public Logout(IUserService users)
    {
        _users = users;
    }

    /// <summary>Must work with an expired access token; the refresh cookie proves ownership.</summary>
    [Function("Logout")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "cms/admin/auth/logout")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        var body = new RefreshTokenRequest { RefreshToken = HttpResponses.ReadRefreshTokenCookie(req) };

        var result = await _users.LogoutAsync(body, cancellationToken);

        HttpResponses.ClearRefreshTokenCookie(req);

        return result.IsSuccess
            ? new NoContentResult()
            : ProblemResults.FromError(result.Error!);
    }
}
