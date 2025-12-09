using System.Net;

namespace Domain.Exceptions;

public class ConflictException : IdentiverseException
{
    public ConflictException(string message) : base(message, HttpStatusCode.Conflict, "Conflict")
    {
    }
}