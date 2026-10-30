using System.Net;

namespace ProductManagement.Application.Common.Exceptions;

public class InsufficientStockException(int productId, uint requested)
    : ServiceException($"Cannot decrement stock by {requested} for product {productId}.", HttpStatusCode.Conflict);