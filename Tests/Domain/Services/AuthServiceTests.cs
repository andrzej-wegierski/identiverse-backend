using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using Domain.Abstractions;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Models;
using Domain.Services;
using Microsoft.Extensions.Options;
using Moq;

namespace Tests.Domain.Services;

public class AuthServiceTests
{
    private static IOptions<JwtOptions> CreateJwtOptions()
    {
        return Options.Create(new JwtOptions
        {
            Issuer = "identiverse.test",
            Audience = "identiverse.aud",
            SigningKey = Convert.ToBase64String(Encoding.UTF8.GetBytes("super_secret_signing_key_12345")),
            ExpiryMinutes = 60
        });
    }

    private static AuthService CreateSut(Mock<IUserRepository> repo)
        => new(repo.Object, CreateJwtOptions());

    [Test]
    public async Task RegisterAsync_Succeeds_With_Unique_Username_And_Email()
    {
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.IsUsernameTakenAsync("newuser", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        repo.Setup(r => r.IsEmailTakenAsync("new@user.com", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var reg = new RegisterUserDto { Username = "newuser", Email = "new@user.com", Password = "P@ssword1234" };

        repo.Setup(r => r.RegisterUserAsync(reg, It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(10)
            .Callback<RegisterUserDto, byte[], byte[], CancellationToken>((_, hash, salt, _) =>
            {
                Assert.That(hash, Is.Not.Null);
                Assert.That(hash.Length, Is.GreaterThan(0));
                Assert.That(salt, Is.Not.Null);
                Assert.That(salt.Length, Is.GreaterThan(0));
            });

        repo.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(new UserDto
        {
            Id = 10, Username = "newuser", Email = "new@user.com", Role = UserRole.User, PersonId = null
        });

        var sut = CreateSut(repo);
        var result = await sut.RegisterAsync(reg);

        Assert.That(result.AccessToken, Is.Not.Null.And.Not.Empty);
        Assert.That(result.User, Is.Not.Null);
        Assert.That(result.User!.Username, Is.EqualTo("newuser"));
    }

    [Test]
    public void RegisterAsync_Throws_When_Username_Already_Taken()
    {
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.IsUsernameTakenAsync("taken", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var sut = CreateSut(repo);
        var reg = new RegisterUserDto { Username = "taken", Email = "x@x.com", Password = "P@ssword1234" };
        Assert.ThrowsAsync<ConflictException>(() => sut.RegisterAsync(reg));
    }

    [Test]
    public void RegisterAsync_Throws_When_Email_Already_Taken()
    {
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.IsUsernameTakenAsync("ok", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        repo.Setup(r => r.IsEmailTakenAsync("dup@x.com", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var sut = CreateSut(repo);
        var reg = new RegisterUserDto { Username = "ok", Email = "dup@x.com", Password = "P@ssword1234" };
        Assert.ThrowsAsync<ConflictException>(() => sut.RegisterAsync(reg));
    }

    [Test]
    public async Task LoginAsync_Succeeds_With_Valid_Username_And_Password()
    {
        var repo = new Mock<IUserRepository>();
        var password = "P@ssword1234";
        var salt = RandomNumberGenerator.GetBytes(16);
        // Use the same hashing as service uses
        var hash = Convert.ToBase64String(Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32));

        var user = new UserDto { Id = 5, Username = "user", Email = "u@e.com", Role = UserRole.Admin, PersonId = 12 };
        repo.Setup(r => r.GetAuthByUserNameOrEmailAsync("user", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthUserData
            {
                User = user,
                PasswordHash = hash,
                PasswordSalt = Convert.ToBase64String(salt)
            });

        var sut = CreateSut(repo);
        var res = await sut.LoginAsync(new LoginUserDto { UsernameOrEmail = "user", Password = password });

        Assert.That(res.AccessToken, Is.Not.Empty);
        Assert.That(res.User!.Id, Is.EqualTo(5));

        var token = new JwtSecurityTokenHandler().ReadJwtToken(res.AccessToken);
        Assert.That(token.Issuer, Is.EqualTo("identiverse.test"));
        Assert.That(token.Audiences, Does.Contain("identiverse.aud"));
        Assert.That(token.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value, Is.EqualTo("5"));
        Assert.That(token.Claims.First(c => c.Type == "personId").Value, Is.EqualTo("12"));
    }

    [Test]
    public void LoginAsync_Throws_When_User_Not_Found()
    {
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetAuthByUserNameOrEmailAsync("missing", It.IsAny<CancellationToken>())).ReturnsAsync((AuthUserData?)null);
        var sut = CreateSut(repo);
        Assert.ThrowsAsync<UnauthorizedIdentiverseException>(() => sut.LoginAsync(new LoginUserDto { UsernameOrEmail = "missing", Password = "x" }));
    }

    [Test]
    public void LoginAsync_Throws_When_Password_Invalid()
    {
        var repo = new Mock<IUserRepository>();
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Convert.ToBase64String(Rfc2898DeriveBytes.Pbkdf2("correct", salt, 100_000, HashAlgorithmName.SHA256, 32));
        repo.Setup(r => r.GetAuthByUserNameOrEmailAsync("user", It.IsAny<CancellationToken>())).ReturnsAsync(new AuthUserData
        {
            User = new UserDto { Id = 1, Username = "user", Email = "e@e.com", Role = UserRole.User },
            PasswordHash = hash,
            PasswordSalt = Convert.ToBase64String(salt)
        });
        var sut = CreateSut(repo);
        Assert.ThrowsAsync<UnauthorizedIdentiverseException>(() => sut.LoginAsync(new LoginUserDto { UsernameOrEmail = "user", Password = "wrong" }));
    }
}
