using System.Net;
using Tahdeera.Domain.Common;

namespace Tahdeera.Domain.Exceptions;

public class DomainValidationException : DomainException
{
    public DomainValidationException(string errorCode, string message = "Validation failed.", HttpStatusCode statusCode = HttpStatusCode.BadRequest)
        : base(errorCode, message, statusCode)
    {
    }
}
