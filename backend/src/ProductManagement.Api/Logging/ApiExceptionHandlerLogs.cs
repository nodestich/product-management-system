using Microsoft.AspNetCore.Http;

using Microsoft.Extensions.Logging;

namespace ProductManagement.Api.Logging;

internal static partial class ApiExceptionHandlerLogs
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Unhandled exception for {Method} {Path}. TraceId {TraceId}")]
    public static partial void UnhandledException(ILogger logger, Exception exception, string method, PathString path, string traceId);


    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Request rejected with {StatusCode} for {Method} {Path}: {Reason}")]
    public static partial void RequestRejected(ILogger logger, int statusCode, string method, PathString path, string reason);

}
