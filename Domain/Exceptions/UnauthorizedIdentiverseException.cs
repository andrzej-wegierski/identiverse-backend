using System.Net;

namespace Domain.Exceptions;

public class UnauthorizedIdentiverseException : IdentiverseException
{
    public UnauthorizedIdentiverseException(string message) : base(message, HttpStatusCode.Unauthorized, "Unauthorized")
    {
    }
    
}