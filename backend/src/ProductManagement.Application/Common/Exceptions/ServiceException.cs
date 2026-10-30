using System.Net;

namespace ProductManagement.Application.Common.Exceptions;

public abstract class ServiceException(string message, HttpStatusCode statusCode) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}