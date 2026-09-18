using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Users;

public class DisableUser
{
    private readonly IUserService _users;

    public DisableUser(IUserService users)
    {
        _users = users;
    }

    [Function("DisableUser")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "cms/admin/users/{id:guid}/disable")] HttpRequest req,
        Guid id,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        var result = await _users.DisableAsync(id, cancellationToken);

        return result.IsSuccess
            ? new OkObjectResult(result.Value)
            : ProblemResults.FromError(result.Error!);
    }
}
