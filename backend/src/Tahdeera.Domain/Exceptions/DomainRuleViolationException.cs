using System.Net;
using Tahdeera.Domain.Common;

namespace Tahdeera.Domain.Exceptions;

public class DomainRuleViolationException : DomainException
{
    public DomainRuleViolationException(string errorCode, string message = "Domain rule violation.", HttpStatusCode statusCode = HttpStatusCode.BadRequest)
        : base(errorCode, message, statusCode)
    {
    }
}
