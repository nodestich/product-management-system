using System.Net;

namespace ProductManagement.Application.Common.Exceptions;

public class InvalidSearchTermException()
    : ServiceException("A search term is required.", HttpStatusCode.BadRequest);
