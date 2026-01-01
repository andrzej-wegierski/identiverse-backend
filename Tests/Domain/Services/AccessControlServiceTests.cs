using Domain.Abstractions;
using Domain.Exceptions;
using Domain.Services;
using Moq;

namespace Tests.Domain.Services;

public class AccessControlServiceTests
{
    private readonly Mock<ICurrentUserContext> _userMock = new();
    private readonly Mock<IIdentityProfileRepository> _profilesMock = new();
    private readonly Mock<IPersonRepository> _personsMock = new();
    private AccessControlService _sut;

    [SetUp]
    public void SetUp()
    {
        _sut = new AccessControlService(_userMock.Object, _profilesMock.Object, _personsMock.Object);
    }

    [Test]
    public async Task CanAccessPersonAsync_ThrowsForbidden_When_NotOwner_And_NotAdmin()
    {
        // Arrange
        int personId = 10;
        int currentUserId = 1;
        int ownerUserId = 2;

        _userMock.SetupGet(u => u.IsAuthenticated).Returns(true);
        _userMock.SetupGet(u => u.IsAdmin).Returns(false);
        _userMock.SetupGet(u => u.UserId).Returns(currentUserId);
        
        _personsMock.Setup(r => r.GetUserIdByPersonIdAsync(personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ownerUserId);

        // Act & Assert
        var ex = Assert.ThrowsAsync<ForbiddenException>(() => _sut.CanAccessPersonAsync(personId));
        Assert.That(ex.Message, Is.EqualTo("User has no access to this person"));
    }

    [Test]
    public async Task CanAccessPersonAsync_Succeeds_When_IsAdmin()
    {
        // Arrange
        int personId = 10;

        _userMock.SetupGet(u => u.IsAuthenticated).Returns(true);
        _userMock.SetupGet(u => u.IsAdmin).Returns(true);

        // Act & Assert
        await _sut.CanAccessPersonAsync(personId);
        
        _personsMock.Verify(r => r.GetUserIdByPersonIdAsync(personId, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task CanAccessPersonAsync_Succeeds_When_IsOwner()
    {
        // Arrange
        int personId = 10;
        int currentUserId = 1;

        _userMock.SetupGet(u => u.IsAuthenticated).Returns(true);
        _userMock.SetupGet(u => u.IsAdmin).Returns(false);
        _userMock.SetupGet(u => u.UserId).Returns(currentUserId);
        
        _personsMock.Setup(r => r.GetUserIdByPersonIdAsync(personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentUserId);

        // Act & Assert
        await _sut.CanAccessPersonAsync(personId);
    }

    [Test]
    public void CanAccessPersonAsync_ThrowsUnauthorized_When_NotAuthenticated()
    {
        // Arrange
        _userMock.SetupGet(u => u.IsAuthenticated).Returns(false);

        // Act & Assert
        Assert.ThrowsAsync<UnauthorizedIdentiverseException>(() => _sut.CanAccessPersonAsync(10));
    }
}
