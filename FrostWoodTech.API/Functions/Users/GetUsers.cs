using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.Enums;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Users;

/// <summary>Super admin check lives in UserService.</summary>
public class GetUsers
{
    private readonly IUserService _users;

    public GetUsers(IUserService users)
    {
        _users = users;
    }

    [Function("GetUsers")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cms/admin/users")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        if (!QueryParameters.TryReadEnum<UserStatus>(req, "status", out var status))
        {
            return ProblemResults.BadRequest("validation_failed", "Unknown ?status= value.");
        }

        var search = QueryParameters.ReadString(req, "search");
        var (page, pageSize) = QueryParameters.ReadPaging(req);

        var result = await _users.GetAllAsync(search, status, page, pageSize, cancellationToken);

        return result.IsSuccess
            ? new OkObjectResult(result.Value)
            : ProblemResults.FromError(result.Error!);
    }
}
