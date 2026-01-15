using identiverse_backend.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Resend;

namespace Tests.Services;

public class EmailSenderTests
{
    [Test]
    public async Task SendEmailAsync_LogsCorrectInformation()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<EmailSender>>();
        var configMock = new Mock<IConfiguration>();
        var resendMock = new Mock<IResend>();
        configMock.Setup(c => c["Resend:ApiKey"]).Returns("test-key");
        configMock.Setup(c => c["Resend:FromEmail"]).Returns("test@example.com");
        
        var sender = new EmailSender(loggerMock.Object, configMock.Object, resendMock.Object);
        var email = "test@example.com";
        var subject = "Test Subject";
        var message = "Hello World";

        // Act
        await sender.SendEmailAsync(email, subject, message);

        // Assert
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(email) && v.ToString()!.Contains(subject)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
        
        resendMock.Verify(x => x.EmailSendAsync(It.IsAny<EmailMessage>(), default), Times.Once);
    }
}