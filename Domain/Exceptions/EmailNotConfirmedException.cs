using System.Net;

namespace Domain.Exceptions;

public class EmailNotConfirmedException : IdentiverseException
{
    public EmailNotConfirmedException(string message = "Email is not confirmed") 
        : base(message, HttpStatusCode.Forbidden, "Email Not Confirmed", "https://errors.identiverse.dev/email-not-confirmed")
    {
    }
}
