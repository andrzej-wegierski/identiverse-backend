using Database.Entities;
using Database.Identity;
using Domain.Abstractions;
using Domain.Exceptions;
using Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Moq;

namespace Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<SignInManager<ApplicationUser>> _signInManagerMock;
    private readonly Mock<IEmailSender> _emailSenderMock = new();
    private readonly Mock<ILoginThrottle> _throttleMock = new();
    private readonly IOptions<JwtOptions> _jwtOptions;

    public AuthServiceTests()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        
        var contextAccessor = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(
            _userManagerMock.Object, 
            contextAccessor.Object, 
            claimsFactory.Object, 
            null!, null!, null!, null!);

        _jwtOptions = Options.Create(new JwtOptions 
        { 
            SigningKey = "very-long-secret-key-at-least-32-chars-long", 
            Issuer = "test", 
            Audience = "test", 
            ExpiryMinutes = 60 
        });
    }

    [SetUp]
    public void SetUp()
    {
        _userManagerMock.Invocations.Clear();
        _signInManagerMock.Invocations.Clear();
        _emailSenderMock.Invocations.Clear();
        _throttleMock.Invocations.Clear();
    }

    private AuthService CreateSut()
    {
        return new AuthService(
            _userManagerMock.Object,
            _signInManagerMock.Object,
            _jwtOptions,
            _throttleMock.Object,
            _emailSenderMock.Object);
    }

    [Test]
    public async Task ForgotPasswordAsync_WhenUserExistsAndEmailConfirmed_SendsToken()
    {
        // Arrange
        var email = "user@example.com";
        var user = new ApplicationUser { Email = email };
        var token = "reset-token-123";
        var dto = new ForgotPasswordDto { Email = email };

        _userManagerMock.Setup(u => u.FindByEmailAsync(email))
            .ReturnsAsync(user);
        _userManagerMock.Setup(u => u.IsEmailConfirmedAsync(user))
            .ReturnsAsync(true);
        _userManagerMock.Setup(u => u.GeneratePasswordResetTokenAsync(user))
            .ReturnsAsync(token);

        var sut = CreateSut();

        // Act
        await sut.ForgotPasswordAsync(dto);

        // Assert
        _emailSenderMock.Verify(e => e.SendEmailAsync(
            email, 
            "Reset Password", 
            It.Is<string>(s => s.Contains(token))), 
            Times.Once);
    }

    [Test]
    public async Task ForgotPasswordAsync_WhenUserDoesNotExist_DoesNotSendEmail()
    {
        // Arrange
        var email = "nonexistent@example.com";
        _userManagerMock.Setup(u => u.FindByEmailAsync(email))
            .ReturnsAsync((ApplicationUser?)null);
        
        var dto = new ForgotPasswordDto { Email = email };
        var sut = CreateSut();

        // Act
        await sut.ForgotPasswordAsync(dto);

        // Assert
        _emailSenderMock.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _userManagerMock.Verify(u => u.GeneratePasswordResetTokenAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Test]
    public async Task ForgotPasswordAsync_WhenEmailNotConfirmed_DoesNotSendEmail()
    {
        // Arrange
        var email = "unconfirmed@example.com";
        var user = new ApplicationUser { Email = email };
        
        _userManagerMock.Setup(u => u.FindByEmailAsync(email))
            .ReturnsAsync(user);
        _userManagerMock.Setup(u => u.IsEmailConfirmedAsync(user))
            .ReturnsAsync(false);
        
        var dto = new ForgotPasswordDto { Email = email };
        var sut = CreateSut();

        // Act
        await sut.ForgotPasswordAsync(dto);

        // Assert
        _emailSenderMock.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
    
      [Test]
    public async Task ResetPasswordAsync_WhenValidRequest_Succeeds()
    {
        // Arrange
        var dto = new ResetPasswordDto 
        { 
            Email = "user@example.com", 
            Token = "valid-token", 
            NewPassword = "NewSecurePassword123!" 
        };
        var user = new ApplicationUser { Email = dto.Email };

        _userManagerMock.Setup(u => u.FindByEmailAsync(dto.Email))
            .ReturnsAsync(user);
        _userManagerMock.Setup(u => u.ResetPasswordAsync(user, dto.Token, dto.NewPassword))
            .ReturnsAsync(IdentityResult.Success);

        var sut = CreateSut();

        // Act & Assert
        await sut.ResetPasswordAsync(dto); // Should not throw
        
        _userManagerMock.Verify(u => u.ResetPasswordAsync(user, dto.Token, dto.NewPassword), Times.Once);
    }

    [Test]
    public void ResetPasswordAsync_WhenUserNotFound_ThrowsValidationException()
    {
        // Arrange
        var dto = new ResetPasswordDto { Email = "nonexistent@example.com", Token = "any", NewPassword = "any" };
        _userManagerMock.Setup(u => u.FindByEmailAsync(dto.Email))
            .ReturnsAsync((ApplicationUser?)null);
        
        var sut = CreateSut();

        // Act & Assert
        var ex = Assert.ThrowsAsync<ValidationException>(async () => await sut.ResetPasswordAsync(dto));
        Assert.That(ex!.Message, Is.EqualTo("Invalid request"));
    }

    [Test]
    public void ResetPasswordAsync_WhenIdentityFails_ThrowsValidationException()
    {
        // Arrange
        var dto = new ResetPasswordDto { Email = "user@example.com", Token = "invalid", NewPassword = "Password123!" };
        var user = new ApplicationUser { Email = dto.Email };

        _userManagerMock.Setup(u => u.FindByEmailAsync(dto.Email))
            .ReturnsAsync(user);
        _userManagerMock.Setup(u => u.ResetPasswordAsync(user, dto.Token, dto.NewPassword))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid token." }));

        var sut = CreateSut();

        // Act & Assert
        Assert.ThrowsAsync<ValidationException>(async () => await sut.ResetPasswordAsync(dto));
    }
    
     [Test]
    public async Task ResendConfirmationEmailAsync_WhenUserExistsAndUnconfirmed_SendsToken()
    {
        // Arrange
        var email = "unconfirmed@example.com";
        var user = new ApplicationUser { Email = email };
        var token = "confirm-token-123";
        var dto = new ResendConfirmationDto { Email = email };

        _userManagerMock.Setup(u => u.FindByEmailAsync(email))
            .ReturnsAsync(user);
        _userManagerMock.Setup(u => u.IsEmailConfirmedAsync(user))
            .ReturnsAsync(false);
        _userManagerMock.Setup(u => u.GenerateEmailConfirmationTokenAsync(user))
            .ReturnsAsync(token);

        var sut = CreateSut();

        // Act
        await sut.ResendConfirmationEmailAsync(dto);

        // Assert
        _emailSenderMock.Verify(e => e.SendEmailAsync(
            email, 
            "Confirm your email", 
            It.Is<string>(s => s.Contains(token))), 
            Times.Once);
    }

    [Test]
    public async Task ResendConfirmationEmailAsync_WhenUserDoesNotExist_DoesNotSendEmail()
    {
        // Arrange
        var email = "nonexistent@example.com";
        _userManagerMock.Setup(u => u.FindByEmailAsync(email))
            .ReturnsAsync((ApplicationUser?)null);
        
        var dto = new ResendConfirmationDto { Email = email };
        var sut = CreateSut();

        // Act
        await sut.ResendConfirmationEmailAsync(dto);

        // Assert
        _emailSenderMock.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _userManagerMock.Verify(u => u.GenerateEmailConfirmationTokenAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Test]
    public async Task ResendConfirmationEmailAsync_WhenUserAlreadyConfirmed_DoesNotSendEmail()
    {
        // Arrange
        var email = "confirmed@example.com";
        var user = new ApplicationUser { Email = email };
        
        _userManagerMock.Setup(u => u.FindByEmailAsync(email))
            .ReturnsAsync(user);
        _userManagerMock.Setup(u => u.IsEmailConfirmedAsync(user))
            .ReturnsAsync(true);
        
        var dto = new ResendConfirmationDto { Email = email };
        var sut = CreateSut();

        // Act
        await sut.ResendConfirmationEmailAsync(dto);

        // Assert
        _emailSenderMock.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _userManagerMock.Verify(u => u.GenerateEmailConfirmationTokenAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }
}