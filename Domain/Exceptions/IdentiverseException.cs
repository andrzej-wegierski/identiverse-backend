using System.Net;

namespace Domain.Exceptions;

public abstract class IdentiverseException : Exception
{
    public int StatusCode { get; }
    public string? Title { get; }
    public string? Type { get; }

    protected IdentiverseException(string message, HttpStatusCode statusCode, string? title = null, string? type = null)
        : base(message)
    {
        StatusCode = (int)statusCode;
        Title = title;
        Type = type;
    }
}