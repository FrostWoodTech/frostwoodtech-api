using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

using FrostWoodTech.API.Common;

namespace FrostWoodTech.API.Middleware;

/// <summary>Turns unhandled exceptions into a generic problem+json 500.</summary>
public sealed class ExceptionHandlingMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(ILogger<ExceptionHandlingMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation(
                "{FunctionName} was cancelled by the caller.",
                context.FunctionDefinition.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled exception in {FunctionName} ({InvocationId}).",
                context.FunctionDefinition.Name,
                context.InvocationId);

            var httpContext = context.GetHttpContext();
            if (httpContext is null)
            {
                // Not an HTTP trigger: let the host handle it.
                throw;
            }

            if (httpContext.Response.HasStarted)
            {
                throw;
            }

            // Generic on purpose: exception messages can leak secrets. The invocation id links to the log.
            await ProblemResults.WriteAsync(
                httpContext.Response,
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                "internal_error",
                $"The request could not be completed. Invocation id: {context.InvocationId}.");
        }
    }
}
