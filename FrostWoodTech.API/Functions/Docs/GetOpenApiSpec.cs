using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Options;

using FrostWoodTech.API.Docs;

namespace FrostWoodTech.API.Functions.Docs;

public class GetOpenApiSpec
{
    private readonly DocsOptions _options;

    public GetOpenApiSpec(IOptions<DocsOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>Gated on Docs__Enabled; disabled answers 404 so the endpoint isn't confirmed.</summary>
    [Function("GetOpenApiSpec")]
    public IActionResult Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "openapi.yaml")] HttpRequest req)
    {
        ArgumentNullException.ThrowIfNull(req);

        if (!_options.Enabled)
        {
            return new NotFoundResult();
        }

        req.HttpContext.Response.Headers.CacheControl = "no-store";

        return new ContentResult
        {
            Content = OpenApiDocument.WithServers(_options.ServerUrls),
            ContentType = OpenApiDocument.ContentType,
            StatusCode = StatusCodes.Status200OK
        };
    }
}
