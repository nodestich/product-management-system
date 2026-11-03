using System.Net;

namespace ProductManagement.Application.Common.Exceptions;

public class InvalidPaginationException(string message)
    : ServiceException(message, HttpStatusCode.BadRequest);