using Domain.Abstractions;
using Domain.Exceptions;
using Domain.Models;
using Domain.Services;
using Moq;

namespace Tests.Services;

public class PersonServiceTests
{
    private readonly Mock<IPersonRepository> _repo = new();
    private readonly Mock<IIdentityService> _identity = new();
    private readonly Mock<IAccessControlService> _access = new();
    private readonly Mock<ICurrentUserContext> _current = new();
    private PersonService CreateSut()
    {
        _repo.Invocations.Clear();
        _identity.Invocations.Clear();
        _access.Invocations.Clear();
        _current.Invocations.Clear();

        // Allow all access by default for unit tests unless overridden
        _access.Setup(a => a.CanAccessPersonAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _current.SetupGet(c => c.UserId).Returns(5);
        return new PersonService(_repo.Object, _identity.Object, _access.Object, _current.Object);
    }

    [Test]
    public async Task GetPersonsAsync_Returns_List_From_Repo()
    {
        var expected = new List<PersonDto> { new() { Id = 1 }, new() { Id = 2 } };
        _repo.Setup(r => r.GetPersonsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var sut = CreateSut();
        var result = await sut.GetPersonsAsync();

        Assert.That(result, Is.SameAs(expected));
        _repo.Verify(r => r.GetPersonsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetPersonByIdAsync_Passes_Through_Value()
    {
        _repo.Setup(r => r.GetPersonByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PersonDto { Id = 5 });

        var sut = CreateSut();
        var result = await sut.GetPersonByIdAsync(5);

        Assert.That(result!.Id, Is.EqualTo(5));
        _repo.Verify(r => r.GetPersonByIdAsync(5, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetPersonByIdAsync_When_Not_Found_Returns_Null()
    {
        _repo.Setup(r => r.GetPersonByIdAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PersonDto?)null);

        var sut = CreateSut();
        var result = await sut.GetPersonByIdAsync(123);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task CreatePersonAsync_Returns_Value_From_Repo()
    {
        var input = new CreatePersonDto { FirstName = "A", LastName = "B" };
        var expected = new PersonDto { Id = 10 };
        _repo.Setup(r => r.CreatePersonAsync(input, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var sut = CreateSut();
        var result = await sut.CreatePersonAsync(input);

        Assert.That(result, Is.SameAs(expected));
        _repo.Verify(r => r.CreatePersonAsync(input, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdatePersonAsync_Passes_Through_Value_And_Null()
    {
        var update = new UpdatePersonDto { FirstName = "N", LastName = "L" };
        _repo.Setup(r => r.UpdatePersonAsync(1, update, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PersonDto { Id = 1 });
        var sut = CreateSut();
        var ok = await sut.UpdatePersonAsync(1, update);
        Assert.That(ok!.Id, Is.EqualTo(1));

        _repo.Setup(r => r.UpdatePersonAsync(2, update, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PersonDto?)null);
        var notFound = await sut.UpdatePersonAsync(2, update);
        Assert.That(notFound, Is.Null);
    }

    [Test]
    public async Task DeletePersonAsync_Passes_Through_Boolean()
    {
        _repo.Setup(r => r.DeletePersonAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repo.Setup(r => r.DeletePersonAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = CreateSut();
        Assert.That(await sut.DeletePersonAsync(1), Is.True);
        Assert.That(await sut.DeletePersonAsync(2), Is.False);
    }

    [Test]
    public async Task CreatePersonForCurrentUserAsync_Creates_And_Links_To_Current_User()
    {
        var input = new CreatePersonDto { FirstName = "John", LastName = "Doe" };
        var created = new PersonDto { Id = 123, FirstName = "John", LastName = "Doe" };
        _repo.Setup(r => r.CreatePersonAsync(input, It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);
        _current.SetupGet(c => c.UserId).Returns(5);
        _identity.Setup(u => u.GetUserByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserDto { Id = 5, PersonId = null });
        _identity.Setup(u => u.LinkPersonToUserAsync(5, 123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateSut();
        var result = await sut.CreatePersonForCurrentUserAsync(input);

        Assert.That(result, Is.SameAs(created));
        _repo.Verify(r => r.CreatePersonAsync(input, It.IsAny<CancellationToken>()), Times.Once);
        _identity.Verify(u => u.LinkPersonToUserAsync(5, 123, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void CreatePersonForCurrentUserAsync_When_Link_Fails_Throws()
    {
        var input = new CreatePersonDto { FirstName = "A", LastName = "B" };
        _repo.Setup(r => r.CreatePersonAsync(input, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PersonDto { Id = 7 });
        _current.SetupGet(c => c.UserId).Returns(9);
        _identity.Setup(u => u.GetUserByIdAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserDto { Id = 9, PersonId = null });
        _identity.Setup(u => u.LinkPersonToUserAsync(9, 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = CreateSut();
        Assert.ThrowsAsync<InvalidOperationException>(async () => await sut.CreatePersonForCurrentUserAsync(input));
    }
    
    [Test]
    public async Task CreatePersonForCurrentUserAsync_When_Person_Already_Exists_Throws_ConflictException()
    {
        // Arrange
        var input = new CreatePersonDto { FirstName = "John", LastName = "Doe" };
        var userId = 5;
        var existingUser = new UserDto { Id = userId, PersonId = 999 }; // Already has a Person
    
        _current.SetupGet(c => c.UserId).Returns(userId);
        _identity.Setup(u => u.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        var sut = CreateSut();

        // Act & Assert
        var ex = Assert.ThrowsAsync<ConflictException>(async () => 
            await sut.CreatePersonForCurrentUserAsync(input));
    
        Assert.That(ex!.Message, Is.EqualTo("Person already exists for this user. Update instead."));
    
        // Verify no side effects
        _repo.Verify(r => r.CreatePersonAsync(It.IsAny<CreatePersonDto>(), It.IsAny<CancellationToken>()), Times.Never);
        _identity.Verify(u => u.LinkPersonToUserAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
