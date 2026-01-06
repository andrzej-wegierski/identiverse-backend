using Database;
using Database.Entities;
using Domain.Abstractions;
using Domain.Enums;
using Domain.Models;
using Domain.Services;
using identiverse_backend.Controllers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Tests.PublicApi.Controllers;

public class AdminControllerTests
{
    private readonly Mock<IPersonService> _persons = new();
    private Mock<UserManager<ApplicationUser>> _userManagerMock = null!;

    [SetUp]
    public void SetUp()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private AdminController CreateSut() => new(_persons.Object, _userManagerMock.Object);

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
    
    [Test]
    public async Task GetAllUsers_Returns_Ok_With_AdminUserDto_List()
    {
        // Arrange
        var usersList = new List<ApplicationUser> 
        { 
            new() { Id = 1, UserName = "admin", Email = "admin@test.com", PersonId = 10 } 
        };

        var options = new DbContextOptionsBuilder<IdentiverseDbContext>()
            .UseInMemoryDatabase(databaseName: "AdminControllerTests_GetAllUsers")
            .Options;

        using var dbContext = new IdentiverseDbContext(options);
        dbContext.Users.AddRange(usersList);
        await dbContext.SaveChangesAsync();

        var store = new Microsoft.AspNetCore.Identity.EntityFrameworkCore.UserStore<ApplicationUser, IdentityRole<int>, IdentiverseDbContext, int>(dbContext);
        var userManager = new UserManager<ApplicationUser>(store, null!, new PasswordHasher<ApplicationUser>(), null!, null!, null!, null!, null!, null!);
        
        var controller = new AdminController(_persons.Object, userManager);

        // Act
        var action = await controller.GetAllUsers(default);

        // Assert
        Assert.That(action.Result, Is.InstanceOf<OkObjectResult>());
        var ok = (OkObjectResult)action.Result!;
        var resultList = (List<AdminUserDto>)ok.Value!;
    
        Assert.That(resultList, Has.Count.EqualTo(1));
        Assert.That(resultList[0].Username, Is.EqualTo("admin"));
    }
}
