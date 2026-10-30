using Microsoft.Extensions.Logging;

namespace ProductManagement.Application.Common.Logging;

public static partial class ProductServiceLogs
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Product with id {Id} created.")]
    public static partial void ProductCreated(ILogger logger, int id);


    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Failed to fetch product with id {Id}.")]
    public static partial void ProductNotFound(ILogger logger, int id);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Product with id {Id} updated.")]
    public static partial void ProductUpdated(ILogger logger, int id);

    [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Product with id {Id} deleted.")]
    public static partial void ProductDeleted(ILogger logger, int id);


}
