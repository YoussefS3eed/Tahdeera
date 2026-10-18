using System.Net;

namespace Tahdeera.Application.Common.Exceptions;

public class UnauthorizedException : Exception
{
    public string ErrorCode { get; }
    public HttpStatusCode StatusCode => HttpStatusCode.Unauthorized;

    public UnauthorizedException(string message = "You are not authorized to perform this operation.", string errorCode = "UNAUTHORIZED")
        : base(message)
    {
        ErrorCode = errorCode;
    }
}
