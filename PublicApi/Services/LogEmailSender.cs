
using Domain.Abstractions;

namespace identiverse_backend.Services;

public class LogEmailSender : IEmailSender
{
    private readonly ILogger<LogEmailSender> _logger;

    public LogEmailSender(ILogger<LogEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        _logger.LogInformation(
            "Sending Email:\nTo: {Email}\nSubject: {Subject}\nContent: {Content}",
            email,
            subject,
            htmlMessage);
        
        return Task.CompletedTask;
    }
}