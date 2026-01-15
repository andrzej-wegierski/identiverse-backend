
using Domain.Abstractions;
using Resend;

namespace identiverse_backend.Services;

public class EmailSender : IEmailSender
{
    private readonly ILogger<EmailSender> _logger;
    private readonly IResend _resend;
    private readonly string _fromEmail;

    public EmailSender(ILogger<EmailSender> logger, IConfiguration configuration)
    {
        _logger = logger;
        var apiKey = configuration["Resend:ApiKey"] ?? throw new InvalidOperationException("Resend API Key is missing");
        _fromEmail = configuration["Resend:FromEmail"] ?? "onboarding@resend.dev";
        _resend = ResendClient.Create(apiKey);
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        _logger.LogInformation(
            "Sending Email via Resend:\nTo: {Email}\nSubject: {Subject}",
            email,
            subject);

        try
        {
            var message = new EmailMessage
            {
                From = _fromEmail,
                To = email,
                Subject = subject,
                HtmlBody = htmlMessage
            };

            await _resend.EmailSendAsync(message);
            _logger.LogInformation("Email sent successfully to {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", email);
            throw;
        }
    }
}