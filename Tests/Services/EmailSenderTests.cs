using identiverse_backend.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests.Services;

public class EmailSenderTests
{
    [Test]
    public async Task SendEmailAsync_LogsCorrectInformation()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<EmailSender>>();
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["Resend:ApiKey"]).Returns("test-key");
        configMock.Setup(c => c["Resend:FromEmail"]).Returns("test@example.com");
        
        var sender = new EmailSender(loggerMock.Object, configMock.Object);
        var email = "test@example.com";
        var subject = "Test Subject";
        var message = "Hello World";

        // Act & Assert (it will try to send via Resend and probably fail because of invalid key, but we check if it logs)
        try 
        {
            await sender.SendEmailAsync(email, subject, message);
        }
        catch
        {
            // Ignore Resend client errors during test
        }

        // Assert
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(email) && v.ToString()!.Contains(subject)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}