using Domain.Models;
using Domain.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using identiverse_backend.Controllers;

namespace Tests.Controllers;

public class PersonsControllerTests
{
    private readonly Mock<IPersonService> _service = new();

    private PersonsController CreateSut() => new(_service.Object);

    [Test]
    public async Task GetPersons_Returns_Ok_With_List()
    {
        var list = new List<PersonDto> { new() { Id = 1 } };
        _service.Setup(s => s.GetPersonsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);

        var controller = CreateSut();
        var action = await controller.GetPersons();

        Assert.That(action.Result, Is.InstanceOf<OkObjectResult>());
        var ok = (OkObjectResult)action.Result!;
        Assert.That(ok.Value, Is.SameAs(list));
    }

    [Test]
    public async Task GetPersonById_Returns_Ok_When_Found()
    {
        var dto = new PersonDto { Id = 5 };
        _service.Setup(s => s.GetPersonByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var controller = CreateSut();
        var action = await controller.GetPersonById(5);

        Assert.That(action.Result, Is.InstanceOf<OkObjectResult>());
        var ok = (OkObjectResult)action.Result!;
        Assert.That(ok.Value, Is.SameAs(dto));
    }

    [Test]
    public async Task GetPersonById_Returns_NotFound_When_Missing()
    {
        _service.Setup(s => s.GetPersonByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PersonDto?)null);

        var controller = CreateSut();
        var action = await controller.GetPersonById(999);
        Assert.That(action.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task CreatePerson_Returns_CreatedAt_With_Payload()
    {
        var create = new CreatePersonDto { FirstName = "A", LastName = "B" };
        var created = new PersonDto { Id = 42 };
        _service.Setup(s => s.CreatePersonAsync(create, It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);

        var controller = CreateSut();
        var action = await controller.CreatePerson(create);

        Assert.That(action.Result, Is.InstanceOf<CreatedAtActionResult>());
        var result = (CreatedAtActionResult)action.Result!;
        Assert.That(result.ActionName, Is.EqualTo(nameof(PersonsController.GetPersonById)));
        Assert.That(result.RouteValues!["id"], Is.EqualTo(created.Id));
        Assert.That(result.Value, Is.SameAs(created));
    }

    [Test]
    public async Task UpdatePerson_Returns_Ok_When_Updated()
    {
        var update = new UpdatePersonDto { FirstName = "N", LastName = "L" };
        var updated = new PersonDto { Id = 7 };
        _service.Setup(s => s.UpdatePersonAsync(7, update, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updated);

        var controller = CreateSut();
        var action = await controller.UpdatePerson(7, update);
        Assert.That(action.Result, Is.InstanceOf<OkObjectResult>());
        var ok = (OkObjectResult)action.Result!;
        Assert.That(ok.Value, Is.SameAs(updated));
    }

    [Test]
    public async Task UpdatePerson_Returns_NotFound_When_Missing()
    {
        var update = new UpdatePersonDto { FirstName = "N", LastName = "L" };
        _service.Setup(s => s.UpdatePersonAsync(1, update, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PersonDto?)null);

        var controller = CreateSut();
        var action = await controller.UpdatePerson(1, update);
        Assert.That(action.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task DeletePerson_Returns_NoContent_When_Deleted()
    {
        _service.Setup(s => s.DeletePersonAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var controller = CreateSut();
        var action = await controller.DeletePerson(1);
        Assert.That(action, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task DeletePerson_Returns_NotFound_When_Missing()
    {
        _service.Setup(s => s.DeletePersonAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var controller = CreateSut();
        var action = await controller.DeletePerson(2);
        Assert.That(action, Is.InstanceOf<NotFoundResult>());
    }
}
