using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

using FrostWoodTech.API.Common;

namespace FrostWoodTech.API.Middleware;

/// <summary>Outermost, so 401/500 responses get CORS headers too. OPTIONS preflight is answered by the Functions host.</summary>
public sealed class CorsMiddleware : IFunctionsWorkerMiddleware
{
    private readonly CorsOptions _options;

    public CorsMiddleware(IOptions<CorsOptions> options)
    {
        _options = options.Value;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var httpContext = context.GetHttpContext();
        if (httpContext is null)
        {
            await next(context);
            return;
        }

        var origin = httpContext.Request.Headers.Origin.ToString();

        // Only echo a configured origin.
        if (!string.IsNullOrEmpty(origin) && _options.Origins.Contains(origin, StringComparer.OrdinalIgnoreCase))
        {
            var headers = httpContext.Response.Headers;
            headers[HeaderNames.AccessControlAllowOrigin] = origin;
            headers[HeaderNames.AccessControlAllowCredentials] = "true";
            headers[HeaderNames.AccessControlExposeHeaders] = "ETag";

            // Public GETs are cached, so the cache must vary by origin.
            headers.Append(HeaderNames.Vary, HeaderNames.Origin);
        }

        await next(context);
    }
}
