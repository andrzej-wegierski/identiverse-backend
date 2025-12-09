using System.Net;

namespace Domain.Exceptions;

public class ValidationException : IdentiverseException 
{
    public ValidationException(string message) 
        : base(message, HttpStatusCode.BadRequest, "Validation Error")
    {
    }
}