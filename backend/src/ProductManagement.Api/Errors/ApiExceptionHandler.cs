using System.Diagnostics;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

using ProductManagement.Api.Logging;
using ProductManagement.Application.Common.Exceptions;
using ProductManagement.Domain.Common;

namespace ProductManagement.Api.Errors;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var status = exception switch
        {
            ServiceException service => (int)service.StatusCode,
            DomainException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        if (status >= StatusCodes.Status500InternalServerError)
        {
            ApiExceptionHandlerLogs.UnhandledException(logger, exception,
                httpContext.Request.Method, httpContext.Request.Path, traceId);
        }
        else
        {
            ApiExceptionHandlerLogs.RequestRejected(logger, status,
                httpContext.Request.Method, httpContext.Request.Path, exception.Message);
        }

        var problem = new ProblemDetails
        {
            Status = status,
            Title = TitleFor(status),
            Detail = status >= StatusCodes.Status500InternalServerError ? null : exception.Message,
            Instance = httpContext.Request.Path
        };

        problem.Extensions["traceId"] = traceId;

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

        return true;
    }

    private static string TitleFor(int status) => status switch
    {
        StatusCodes.Status400BadRequest => "Invalid request",
        StatusCodes.Status404NotFound => "Resource not found",
        StatusCodes.Status409Conflict => "Request conflicts with current state",
        _ => "An unexpected error occurred"
    };
}