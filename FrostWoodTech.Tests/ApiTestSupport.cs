using System.Reflection;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.Enums;

namespace FrostWoodTech.Tests;

/// <summary>Stub for any service interface: records Site arguments and returns empty successes.</summary>
public class RecordingServiceProxy : DispatchProxy
{
    public List<Site> SitesReceived { get; } = [];

    public static (T Service, RecordingServiceProxy Recorder) Create<T>() where T : class
    {
        var service = Create<T, RecordingServiceProxy>();

        return (service, (RecordingServiceProxy)(object)service);
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        SitesReceived.AddRange(args?.OfType<Site>() ?? []);

        var returnType = targetMethod!.ReturnType;

        return returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>)
            ? typeof(Task).GetMethod(nameof(Task.FromResult))!
                .MakeGenericMethod(returnType.GenericTypeArguments[0])
                .Invoke(null, [EmptyValue(returnType.GenericTypeArguments[0])])
            : null;
    }

    private static object? EmptyValue(Type type)
    {
        if (!type.IsGenericType)
        {
            return null;
        }

        var definition = type.GetGenericTypeDefinition();
        var item = type.GenericTypeArguments[0];

        if (definition == typeof(IReadOnlyList<>))
        {
            return Array.CreateInstance(item, 0);
        }

        if (definition == typeof(PagedResult<>))
        {
            var paged = Activator.CreateInstance(type)!;
            type.GetProperty(nameof(PagedResult<object>.Items))!.SetValue(paged, Array.CreateInstance(item, 0));

            return paged;
        }

        if (definition == typeof(ServiceResult<>))
        {
            return type.GetMethod(nameof(ServiceResult<object>.Success))!.Invoke(null, [null]);
        }

        return null;
    }
}

public static class ApiAssert
{
    public static HttpRequest Request(string? query = null)
    {
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString(query is null ? string.Empty : $"?{query}");

        return context.Request;
    }

    public static void Problem(IActionResult result, int status, string code)
    {
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(status, objectResult.StatusCode);

        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(code, problem.Extensions["code"]);
    }
}
