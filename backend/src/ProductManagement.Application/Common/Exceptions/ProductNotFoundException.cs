using System.Net;

namespace ProductManagement.Application.Common.Exceptions;

public class ProductNotFoundException(object key)
    : ServiceException($"Product with id '{key}' was not found.", HttpStatusCode.NotFound);