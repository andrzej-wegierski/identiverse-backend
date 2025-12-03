using Domain.Abstractions;
using Domain.Models;
using Domain.Services;
using Moq;

namespace Tests.Services;

public class PersonServiceTests
{
    private readonly Mock<IPersonRepository> _repo = new();
    private PersonService CreateSut() => new(_repo.Object);

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
}
