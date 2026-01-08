using identiverse_backend.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests.Services;

public class LogEmailSenderTests
{
    [Test]
    public async Task SendEmailAsync_LogsCorrectInformation()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<LogEmailSender>>();
        var sender = new LogEmailSender(loggerMock.Object);
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
            Times.Once);
    }
}