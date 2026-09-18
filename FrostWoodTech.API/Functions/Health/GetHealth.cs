using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using FrostWoodTech.API.Data;

namespace FrostWoodTech.API.Functions.Health;

public class GetHealth
{
    private readonly FrostWoodTechDbContext _db;
    private readonly ILogger<GetHealth> _logger;

    public GetHealth(FrostWoodTechDbContext db, ILogger<GetHealth> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>Readiness check that queries the database; the first call after Neon idles takes about a second.</summary>
    [Function("GetHealth")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        req.HttpContext.Response.Headers.CacheControl = "no-store";

        try
        {
            await _db.Database.ExecuteSqlRawAsync("select 1", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check could not reach the database.");

            return new ObjectResult(new { status = "unhealthy", database = "unreachable" })
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable
            };
        }

        return new OkObjectResult(new { status = "healthy", database = "ok" });
    }
}
