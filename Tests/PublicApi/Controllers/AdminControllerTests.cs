using Domain.Abstractions;
using Domain.Models;
using Domain.Services;
using identiverse_backend.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Tests.PublicApi.Controllers;

public class AdminControllerTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPersonService> _persons = new();

    private AdminController CreateSut() => new(_users.Object, _persons.Object);

    [Test]
    public async Task GetAllUsers_Returns_Ok_With_List()
    {
        var list = new List<UserDto> { new() { Id = 1, Username = "u", Email = "e@e.com", Role = "User" } };
        _users.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(list);
        var controller = CreateSut();
        var action = await controller.GetAllusers();
        Assert.That(action.Result, Is.InstanceOf<OkObjectResult>());
        var ok = (OkObjectResult)action.Result!;
        Assert.That(ok.Value, Is.SameAs(list));
    }

    [Test]
    public async Task GetAllPersons_Returns_Ok_With_List()
    {
        var list = new List<PersonDto> { new() { Id = 2 } };
        _persons.Setup(s => s.GetPersonsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(list);
        var controller = CreateSut();
        var action = await controller.GetAllPersons(default);
        Assert.That(action.Result, Is.InstanceOf<OkObjectResult>());
        var ok = (OkObjectResult)action.Result!;
        Assert.That(ok.Value, Is.SameAs(list));
    }
}
