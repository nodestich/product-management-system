using System.Net;

namespace ProductManagement.Application.Common.Exceptions;

public class StockOverflowException(int productId, uint requested)
    : ServiceException($"Cannot increment stock by {requested} for product {productId}. Stock overflows capacity.", HttpStatusCode.Conflict);