using System.Net;

namespace Domain.Exceptions;

public class TooManyRequestsException : IdentiverseException
{
    public TooManyRequestsException(string message)
        : base(message, HttpStatusCode.TooManyRequests, title: "Too Many Requests")
    {
    }
}
