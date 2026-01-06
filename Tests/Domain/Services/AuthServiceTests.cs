using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Database.Entities;
using Domain.Abstractions;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Models;
using Domain.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Tests.Domain.Services;

public class AuthServiceTests
{
    private Mock<UserManager<ApplicationUser>> _userManagerMock = null!;
    private Mock<SignInManager<ApplicationUser>> _signInManagerMock = null!;
    private Mock<ILoginThrottle> _throttleMock = null!;
    private IOptions<JwtOptions> _jwtOptions = null!;

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

        _throttleMock = new Mock<ILoginThrottle>();
        _throttleMock.Setup(t => t.IsAllowedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        _jwtOptions = Options.Create(new JwtOptions
        {
            Issuer = "identiverse.test",
            Audience = "identiverse.aud",
            SigningKey = Convert.ToBase64String(Encoding.UTF8.GetBytes("super_secret_signing_key_12345_67890")),
            ExpiryMinutes = 60
        });
    }

    private AuthService CreateSut()
    {
        return new AuthService(_userManagerMock.Object, _signInManagerMock.Object, _jwtOptions, _throttleMock.Object);
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
    public void LoginAsync_Throws_TooManyRequests_When_Throttled()
    {
        _throttleMock.Setup(t => t.IsAllowedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var sut = CreateSut();
        Assert.ThrowsAsync<TooManyRequestsException>(() => 
            sut.LoginAsync(new LoginUserDto { UsernameOrEmail = "user", Password = "password" }));
    }
}
