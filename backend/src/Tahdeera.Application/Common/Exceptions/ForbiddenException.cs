using System.Net;

namespace Tahdeera.Application.Common.Exceptions;

public class ForbiddenException : Exception
{
    public string ErrorCode { get; }
    public HttpStatusCode StatusCode => HttpStatusCode.Forbidden;

    public ForbiddenException(string errorCode, string message = "You do not have permission to access this resource.")
        : base(message)
    {
        ErrorCode = errorCode;
    }
}
