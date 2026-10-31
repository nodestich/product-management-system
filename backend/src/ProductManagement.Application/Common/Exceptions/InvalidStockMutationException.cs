using System.Net;

namespace ProductManagement.Application.Common.Exceptions;

public class InvalidStockMutationException(int productId, uint requested)
    : ServiceException($"The amount of {requested} is not valid for altering stock for product {productId}.", HttpStatusCode.BadRequest);