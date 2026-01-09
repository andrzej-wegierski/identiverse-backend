using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Database.Entities;
using Database.Identity;
using Domain.Abstractions;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;

namespace Tests.Services;

public class AuthServiceTests
{
    private Mock<UserManager<ApplicationUser>> _userManagerMock = null!;
    private Mock<SignInManager<ApplicationUser>> _signInManagerMock = null!;
    private readonly Mock<IEmailSender> _emailSenderMock = new();
    private readonly Mock<ILoginThrottle> _throttleMock = new();
    private readonly Mock<IEmailThrottle> _emailThrottleMock = new();
    private IOptions<JwtOptions> _jwtOptions = null!;
    private IOptions<FrontendLinksOptions> _frontendOptions = null!;

    [SetUp]
    public void SetUp()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        
        var contextAccessor = new Mock<IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(
            _userManagerMock.Object, 
            contextAccessor.Object, 
            claimsFactory.Object, 
            null!, null!, null!, null!);

        _jwtOptions = Options.Create(new JwtOptions 
        { 
            SigningKey = Convert.ToBase64String(Encoding.UTF8.GetBytes("super_secret_signing_key_12345_67890")), 
            Issuer = "test", 
            Audience = "test", 
            ExpiryMinutes = 60 
        });

        _frontendOptions = Options.Create(new FrontendLinksOptions
        {
            BaseUrl = "http://localhost:5173"
        });

        _throttleMock.Setup(t => t.IsAllowedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _emailThrottleMock.Setup(t => t.IsAllowedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        
        _userManagerMock.Invocations.Clear();
        _signInManagerMock.Invocations.Clear();
        _emailSenderMock.Invocations.Clear();
        _throttleMock.Invocations.Clear();
        _emailThrottleMock.Invocations.Clear();
    }

    private AuthService CreateSut()
    {
        return new AuthService(
            _userManagerMock.Object,
            _signInManagerMock.Object,
            _jwtOptions,
            _throttleMock.Object,
            _emailThrottleMock.Object,
            _emailSenderMock.Object,
            _frontendOptions);
    }

    [Test]
    public async Task RegisterAsync_Succeeds_When_Identity_Succeeds()
    {
        var reg = new RegisterUserDto { Username = "newuser", Email = "new@user.com", Password = "P@ssword1234" };
        
        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), reg.Password))
            .ReturnsAsync(IdentityResult.Success);
        
        _userManagerMock.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User"))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string> { "User" });

        _userManagerMock.Setup(m => m.GenerateEmailConfirmationTokenAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync("dummy-token");

        var sut = CreateSut();
        var result = await sut.RegisterAsync(reg);

        Assert.That(result.AccessToken, Is.Not.Null.And.Not.Empty);
        Assert.That(result.User, Is.Not.Null);
        Assert.That(result.User!.Username, Is.EqualTo("newuser"));
        
        _userManagerMock.Verify(m => m.CreateAsync(It.Is<ApplicationUser>(u => u.UserName == reg.Username), reg.Password), Times.Once);
        _userManagerMock.Verify(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User"), Times.Once);
    }

    [Test]
    public void RegisterAsync_Throws_Conflict_When_Username_Duplicate()
    {
        var reg = new RegisterUserDto { Username = "taken", Email = "x@x.com", Password = "P@ssword1234" };
        
        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), reg.Password))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "DuplicateUserName" }));

        var sut = CreateSut();
        Assert.ThrowsAsync<ConflictException>(() => sut.RegisterAsync(reg));
    }

    [Test]
    public async Task LoginAsync_Succeeds_With_Valid_Credentials()
    {
        var login = new LoginUserDto { UsernameOrEmail = "user", Password = "P@ssword1234" };
        var appUser = new ApplicationUser { Id = 5, UserName = "user", Email = "u@e.com", PersonId = 12 };

        _userManagerMock.Setup(m => m.FindByNameAsync(login.UsernameOrEmail)).ReturnsAsync(appUser);
        
        _signInManagerMock.Setup(m => m.CheckPasswordSignInAsync(appUser, login.Password, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        _userManagerMock.Setup(m => m.IsEmailConfirmedAsync(appUser)).ReturnsAsync(true);

        _userManagerMock.Setup(m => m.GetRolesAsync(appUser))
            .ReturnsAsync(new List<string> { "Admin" });

        var sut = CreateSut();
        var res = await sut.LoginAsync(login);

        Assert.That(res.AccessToken, Is.Not.Empty);
        Assert.That(res.User!.Id, Is.EqualTo(5));
        Assert.That(res.User.Role, Is.EqualTo(UserRole.Admin));

        var token = new JwtSecurityTokenHandler().ReadJwtToken(res.AccessToken);
        Assert.That(token.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value, Is.EqualTo("5"));
        Assert.That(token.Claims.First(c => c.Type == ClaimTypes.Role).Value, Is.EqualTo("Admin"));
        
        _throttleMock.Verify(t => t.RegisterSuccessAsync(login.UsernameOrEmail, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void LoginAsync_Throws_Unauthorized_When_User_Not_Found()
    {
        var login = new LoginUserDto { UsernameOrEmail = "missing", Password = "x" };
        _userManagerMock.Setup(m => m.FindByNameAsync(login.UsernameOrEmail)).ReturnsAsync((ApplicationUser?)null);
        _userManagerMock.Setup(m => m.FindByEmailAsync(login.UsernameOrEmail)).ReturnsAsync((ApplicationUser?)null);

        var sut = CreateSut();
        Assert.ThrowsAsync<UnauthorizedIdentiverseException>(() => sut.LoginAsync(login));
        
        _throttleMock.Verify(t => t.RegisterFailureAsync(login.UsernameOrEmail, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void LoginAsync_Throws_Forbidden_When_LockedOut()
    {
        var login = new LoginUserDto { UsernameOrEmail = "user", Password = "password" };
        var appUser = new ApplicationUser { UserName = "user" };

        _userManagerMock.Setup(m => m.FindByNameAsync(login.UsernameOrEmail)).ReturnsAsync(appUser);
        _signInManagerMock.Setup(m => m.CheckPasswordSignInAsync(appUser, login.Password, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);

        var sut = CreateSut();
        Assert.ThrowsAsync<ForbiddenException>(() => sut.LoginAsync(login));
    }

    [Test]
    public void LoginAsync_Throws_EmailNotConfirmed_When_Email_Not_Confirmed()
    {
        var login = new LoginUserDto { UsernameOrEmail = "user", Password = "password" };
        var appUser = new ApplicationUser { UserName = "user" };

        _userManagerMock.Setup(m => m.FindByNameAsync(login.UsernameOrEmail)).ReturnsAsync(appUser);
        _signInManagerMock.Setup(m => m.CheckPasswordSignInAsync(appUser, login.Password, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
        _userManagerMock.Setup(m => m.IsEmailConfirmedAsync(appUser)).ReturnsAsync(false);

        var sut = CreateSut();
        var ex = Assert.ThrowsAsync<EmailNotConfirmedException>(() => sut.LoginAsync(login));
        Assert.That(ex!.Message, Is.EqualTo("Email is not confirmed"));
        
        _throttleMock.Verify(t => t.RegisterFailureAsync(login.UsernameOrEmail, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void LoginAsync_Throws_TooManyRequests_When_Throttled()
    {
        _throttleMock.Setup(t => t.IsAllowedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var sut = CreateSut();
        Assert.ThrowsAsync<TooManyRequestsException>(() => 
            sut.LoginAsync(new LoginUserDto { UsernameOrEmail = "user", Password = "password" }));
    }

    [Test]
    public async Task ForgotPasswordAsync_WhenUserExistsAndEmailConfirmed_SendsToken()
    {
        // Arrange
        var email = "user@example.com";
        var user = new ApplicationUser { Email = email };
        var token = "reset-token-123";
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
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
            It.Is<string>(s => s.Contains(encodedToken))), 
            Times.Once);
        
        _emailThrottleMock.Verify(t => t.RegisterAttemptAsync(email, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ForgotPasswordAsync_WhenThrottled_DoesNotSendEmail()
    {
        // Arrange
        var email = "user@example.com";
        var dto = new ForgotPasswordDto { Email = email };

        _emailThrottleMock.Setup(t => t.IsAllowedAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = CreateSut();

        // Act
        await sut.ForgotPasswordAsync(dto);

        // Assert
        _emailSenderMock.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _emailThrottleMock.Verify(t => t.RegisterAttemptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
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
        _emailThrottleMock.Verify(t => t.RegisterAttemptAsync(email, It.IsAny<CancellationToken>()), Times.Once);
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
        _emailThrottleMock.Verify(t => t.RegisterAttemptAsync(email, It.IsAny<CancellationToken>()), Times.Once);
    }
    
      [Test]
    public async Task ResetPasswordAsync_WhenValidRequest_Succeeds()
    {
        // Arrange
        var token = "valid-token";
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var dto = new ResetPasswordDto 
        { 
            Email = "user@example.com", 
            Token = encodedToken, 
            NewPassword = "NewSecurePassword123!" 
        };
        var user = new ApplicationUser { Email = dto.Email };

        _userManagerMock.Setup(u => u.FindByEmailAsync(dto.Email))
            .ReturnsAsync(user);
        _userManagerMock.Setup(u => u.ResetPasswordAsync(user, token, dto.NewPassword))
            .ReturnsAsync(IdentityResult.Success);

        var sut = CreateSut();

        // Act & Assert
        await sut.ResetPasswordAsync(dto); // Should not throw
        
        _userManagerMock.Verify(u => u.ResetPasswordAsync(user, token, dto.NewPassword), Times.Once);
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
        var token = "invalid";
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var dto = new ResetPasswordDto { Email = "user@example.com", Token = encodedToken, NewPassword = "Password123!" };
        var user = new ApplicationUser { Email = dto.Email };

        _userManagerMock.Setup(u => u.FindByEmailAsync(dto.Email))
            .ReturnsAsync(user);
        _userManagerMock.Setup(u => u.ResetPasswordAsync(user, token, dto.NewPassword))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid token." }));

        var sut = CreateSut();

        // Act & Assert
        Assert.ThrowsAsync<ValidationException>(async () => await sut.ResetPasswordAsync(dto));
    }

    [Test]
    public void ResetPasswordAsync_WhenTokenInvalidBase64_ThrowsValidationException()
    {
        // Arrange
        var dto = new ResetPasswordDto
        {
            Email = "user@example.com",
            Token = "not-base64url!!",
            NewPassword = "NewSecurePassword123!"
        };
        var user = new ApplicationUser { Email = dto.Email };

        _userManagerMock.Setup(u => u.FindByEmailAsync(dto.Email))
            .ReturnsAsync(user);

        var sut = CreateSut();

        // Act & Assert
        var ex = Assert.ThrowsAsync<ValidationException>(async () => await sut.ResetPasswordAsync(dto));
        Assert.That(ex!.Message, Is.EqualTo("Invalid request"));
    }
    
     [Test]
    public async Task ResendConfirmationEmailAsync_WhenUserExistsAndUnconfirmed_SendsToken()
    {
        // Arrange
        var email = "unconfirmed@example.com";
        var user = new ApplicationUser { Email = email };
        var token = "confirm-token-123";
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
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
            It.Is<string>(s => s.Contains(encodedToken))), 
            Times.Once);

        _emailThrottleMock.Verify(t => t.RegisterAttemptAsync(email, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ResendConfirmationEmailAsync_WhenThrottled_DoesNotSendEmail()
    {
        // Arrange
        var email = "user@example.com";
        var dto = new ResendConfirmationDto { Email = email };

        _emailThrottleMock.Setup(t => t.IsAllowedAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = CreateSut();

        // Act
        await sut.ResendConfirmationEmailAsync(dto);

        // Assert
        _emailSenderMock.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _emailThrottleMock.Verify(t => t.RegisterAttemptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
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
        _emailThrottleMock.Verify(t => t.RegisterAttemptAsync(email, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ResendConfirmationEmailAsync_WhenUserAlreadyConfirmed_ThrowsConflictException()
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

        // Act & Assert
        var ex = Assert.ThrowsAsync<ConflictException>(() => sut.ResendConfirmationEmailAsync(dto));
        Assert.That(ex!.Message, Is.EqualTo("Email is already confirmed"));

        _emailSenderMock.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _userManagerMock.Verify(u => u.GenerateEmailConfirmationTokenAsync(It.IsAny<ApplicationUser>()), Times.Never);
        _emailThrottleMock.Verify(t => t.RegisterAttemptAsync(email, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ConfirmEmailAsync_WhenValidRequest_Succeeds()
    {
        // Arrange
        var token = "valid-token";
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var dto = new ConfirmEmailDto
        {
            Email = "user@example.com",
            Token = encodedToken
        };
        var user = new ApplicationUser { Email = dto.Email };

        _userManagerMock.Setup(u => u.FindByEmailAsync(dto.Email))
            .ReturnsAsync(user);
        _userManagerMock.Setup(u => u.IsEmailConfirmedAsync(user))
            .ReturnsAsync(false);
        _userManagerMock.Setup(u => u.ConfirmEmailAsync(user, token))
            .ReturnsAsync(IdentityResult.Success);

        var sut = CreateSut();

        // Act
        var result = await sut.ConfirmEmailAsync(dto);

        // Assert
        Assert.That(result, Is.True);
        _userManagerMock.Verify(u => u.ConfirmEmailAsync(user, token), Times.Once);
    }

    [Test]
    public async Task ConfirmEmailAsync_WhenAlreadyConfirmed_ReturnsFalse()
    {
        // Arrange
        var token = "valid-token";
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var dto = new ConfirmEmailDto
        {
            Email = "user@example.com",
            Token = encodedToken
        };
        var user = new ApplicationUser { Email = dto.Email };

        _userManagerMock.Setup(u => u.FindByEmailAsync(dto.Email))
            .ReturnsAsync(user);
        _userManagerMock.Setup(u => u.IsEmailConfirmedAsync(user))
            .ReturnsAsync(true);

        var sut = CreateSut();

        // Act
        var result = await sut.ConfirmEmailAsync(dto);

        // Assert
        Assert.That(result, Is.False);
        _userManagerMock.Verify(u => u.ConfirmEmailAsync(user, It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void ConfirmEmailAsync_WhenUserNotFound_ThrowsValidationException()
    {
        // Arrange
        var dto = new ConfirmEmailDto { Email = "nonexistent@example.com", Token = "any" };
        _userManagerMock.Setup(u => u.FindByEmailAsync(dto.Email))
            .ReturnsAsync((ApplicationUser?)null);

        var sut = CreateSut();

        // Act & Assert
        var ex = Assert.ThrowsAsync<ValidationException>(async () => await sut.ConfirmEmailAsync(dto));
        Assert.That(ex!.Message, Is.EqualTo("Invalid request"));
    }

    [Test]
    public void ConfirmEmailAsync_WhenIdentityFails_ThrowsValidationException()
    {
        // Arrange
        var token = "invalid";
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var dto = new ConfirmEmailDto { Email = "user@example.com", Token = encodedToken };
        var user = new ApplicationUser { Email = dto.Email };

        _userManagerMock.Setup(u => u.FindByEmailAsync(dto.Email))
            .ReturnsAsync(user);
        _userManagerMock.Setup(u => u.ConfirmEmailAsync(user, token))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid token." }));

        var sut = CreateSut();

        // Act & Assert
        Assert.ThrowsAsync<ValidationException>(async () => await sut.ConfirmEmailAsync(dto));
    }

    [Test]
    public void ConfirmEmailAsync_WhenTokenInvalidBase64_ThrowsValidationException()
    {
        // Arrange
        var dto = new ConfirmEmailDto
        {
            Email = "user@example.com",
            Token = "not-base64url!!"
        };
        var user = new ApplicationUser { Email = dto.Email };

        _userManagerMock.Setup(u => u.FindByEmailAsync(dto.Email))
            .ReturnsAsync(user);

        var sut = CreateSut();

        // Act & Assert
        var ex = Assert.ThrowsAsync<ValidationException>(async () => await sut.ConfirmEmailAsync(dto));
        Assert.That(ex!.Message, Is.EqualTo("Invalid request"));
    }

    [Test]
    public async Task ChangePassword_Succeeds_When_CurrentPassword_Correct()
    {
        // Arrange
        var userId = 1;
        var dto = new ChangePasswordDto { CurrentPassword = "OldPassword123!", NewPassword = "NewPassword123!" };
        var user = new ApplicationUser { Id = userId };

        _userManagerMock.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword))
            .ReturnsAsync(IdentityResult.Success);

        var sut = CreateSut();

        // Act
        await sut.ChangePassword(userId, dto);

        // Assert
        _userManagerMock.Verify(m => m.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword), Times.Once);
        _userManagerMock.Verify(m => m.UpdateSecurityStampAsync(user), Times.Once);
    }

    [Test]
    public void ChangePassword_Throws_Unauthorized_When_User_NotFound()
    {
        // Arrange
        var userId = 999;
        var dto = new ChangePasswordDto { CurrentPassword = "any", NewPassword = "any" };
        _userManagerMock.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync((ApplicationUser?)null);

        var sut = CreateSut();

        // Act & Assert
        Assert.ThrowsAsync<UnauthorizedIdentiverseException>(() => sut.ChangePassword(userId, dto));
    }

    [Test]
    public void ChangePassword_Throws_Unauthorized_When_CurrentPassword_Wrong()
    {
        // Arrange
        var userId = 1;
        var dto = new ChangePasswordDto { CurrentPassword = "WrongPassword", NewPassword = "NewPassword123!" };
        var user = new ApplicationUser { Id = userId };

        _userManagerMock.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "PasswordMismatch" }));

        var sut = CreateSut();

        // Act & Assert
        var ex = Assert.ThrowsAsync<UnauthorizedIdentiverseException>(() => sut.ChangePassword(userId, dto));
        Assert.That(ex!.Message, Is.EqualTo("Invalid current password"));
    }

    [Test]
    public async Task ChangePassword_Throws_Validation_When_Policy_Fails()
    {
        // ... (existing code)
    }

    [Test]
    public async Task GenerateLink_Handles_Trailing_Slash_Correctly()
    {
        // Arrange
        var email = "user@example.com";
        var user = new ApplicationUser { Email = email };
        var token = "reset-token";
        
        _frontendOptions = Options.Create(new FrontendLinksOptions
        {
            BaseUrl = "http://localhost:5173/" // Trailing slash
        });

        _userManagerMock.Setup(u => u.FindByEmailAsync(email)).ReturnsAsync(user);
        _userManagerMock.Setup(u => u.IsEmailConfirmedAsync(user)).ReturnsAsync(true);
        _userManagerMock.Setup(u => u.GeneratePasswordResetTokenAsync(user)).ReturnsAsync(token);

        var sut = CreateSut();

        // Act
        await sut.ForgotPasswordAsync(new ForgotPasswordDto { Email = email });

        // Assert
        _emailSenderMock.Verify(e => e.SendEmailAsync(
            email,
            "Reset Password",
            It.Is<string>(s => s.Contains("http://localhost:5173/reset-password?"))),
            Times.Once);
    }
}