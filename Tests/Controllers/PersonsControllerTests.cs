using Domain.Models;
using Domain.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using identiverse_backend.Controllers;
using identiverse_backend.Services;

namespace Tests.Controllers;

public class PersonsControllerTests
{
    private readonly Mock<IPersonService> _service = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    private PersonsController CreateSut() => new(_service.Object, _currentUser.Object);

    [Test]
    public async Task GetPersons_Returns_Ok_With_List()
    {
        _currentUser.SetupGet(u => u.IsAdmin).Returns(true);
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
        // Admin can access any person
        _currentUser.SetupGet(u => u.IsAdmin).Returns(true);
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
        _currentUser.SetupGet(u => u.IsAdmin).Returns(true);
        _service.Setup(s => s.GetPersonByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PersonDto?)null);

        var controller = CreateSut();
        var action = await controller.GetPersonById(999);
        Assert.That(action.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task CreatePerson_Returns_CreatedAt_With_Payload()
    {
        // Admin path
        _currentUser.SetupGet(u => u.IsAdmin).Returns(true);
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
        _currentUser.Setup(u => u.IsAdmin).Returns(true);
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
        _currentUser.Setup(u => u.IsAdmin).Returns(true);
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
        _currentUser.SetupGet(u => u.IsAdmin).Returns(true);
        _service.Setup(s => s.DeletePersonAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var controller = CreateSut();
        var action = await controller.DeletePerson(1);
        Assert.That(action, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task DeletePerson_Returns_NotFound_When_Missing()
    {
        _currentUser.SetupGet(u => u.IsAdmin).Returns(true);
        _service.Setup(s => s.DeletePersonAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var controller = CreateSut();
        var action = await controller.DeletePerson(2);
        Assert.That(action, Is.InstanceOf<NotFoundResult>());
    }

    // New access-control tests
    [Test]
    public async Task GetPersonById_Admin_Can_Get_Any_Person()
    {
        _currentUser.SetupGet(u => u.IsAdmin).Returns(true);
        var dto = new PersonDto { Id = 11 };
        _service.Setup(s => s.GetPersonByIdAsync(11, It.IsAny<CancellationToken>())).ReturnsAsync(dto);
        var controller = CreateSut();
        var action = await controller.GetPersonById(11);
        Assert.That(action.Result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetPersonById_NonAdmin_With_Matching_PersonId_Returns_Ok()
    {
        _currentUser.SetupGet(u => u.IsAdmin).Returns(false);
        _currentUser.Setup(u => u.GetCurrentUser()).Returns(new CurrentUser { UserId = 1, Username = "u", Role = "User", PersonId = 7 });
        var dto = new PersonDto { Id = 7 };
        _service.Setup(s => s.GetPersonByIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(dto);
        var controller = CreateSut();
        var action = await controller.GetPersonById(7);
        Assert.That(action.Result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetPersonById_NonAdmin_With_Different_PersonId_Returns_Forbid()
    {
        _currentUser.SetupGet(u => u.IsAdmin).Returns(false);
        _currentUser.Setup(u => u.GetCurrentUser()).Returns(new CurrentUser { UserId = 1, Username = "u", Role = "User", PersonId = 5 });
        var controller = CreateSut();
        var action = await controller.GetPersonById(6);
        Assert.That(action.Result, Is.InstanceOf<ForbidResult>());
    }

    [Test]
    public async Task GetPersonById_NonAdmin_With_No_PersonId_Returns_Forbid()
    {
        _currentUser.SetupGet(u => u.IsAdmin).Returns(false);
        _currentUser.Setup(u => u.GetCurrentUser()).Returns(new CurrentUser { UserId = 1, Username = "u", Role = "User", PersonId = null });
        var controller = CreateSut();
        var action = await controller.GetPersonById(1);
        Assert.That(action.Result, Is.InstanceOf<ForbidResult>());
    }
}
