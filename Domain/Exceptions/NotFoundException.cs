using System.Net;

namespace Domain.Exceptions;

public class NotFoundException : IdentiverseException
{
    public NotFoundException(string message) : base(message, HttpStatusCode.NotFound, title: "Not Found")
    {
    }

}