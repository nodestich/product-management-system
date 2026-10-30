using System.Net;

namespace ProductManagement.Application.Common.Exceptions;

public class InvalidStockRangeException(int min, int max)
    : ServiceException($"The stock range of Min:{min} - Max:{max}is not valid.", HttpStatusCode.BadRequest);