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

    private AdminController CreateSut() => new(_persons.Object);

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
    public async Task GetAllUsers_Returns_Ok_With_AdminUserDto_List_And_Roles()
    {
        // Arrange
        var usersList = new List<ApplicationUser> 
        { 
            new() { Id = 1, UserName = "admin", Email = "admin@test.com", PersonId = 10 },
            new() { Id = 2, UserName = "user", Email = "user@test.com", PersonId = 11 }
        };

        var rolesList = new List<IdentityRole<int>>
        {
            new() { Id = 1, Name = "Admin" },
            new() { Id = 2, Name = "User" }
        };

        var userRolesList = new List<IdentityUserRole<int>>
        {
            new() { UserId = 1, RoleId = 1 },
            new() { UserId = 1, RoleId = 2 },
            new() { UserId = 2, RoleId = 2 }
        };

        var options = new DbContextOptionsBuilder<IdentiverseDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var dbContext = new IdentiverseDbContext(options);
        dbContext.Users.AddRange(usersList);
        dbContext.Roles.AddRange(rolesList);
        dbContext.UserRoles.AddRange(userRolesList);
        await dbContext.SaveChangesAsync();
        
        var controller = new AdminController(_persons.Object);

        // Act
        var action = await controller.GetAllUsers(dbContext, default);

        // Assert
        Assert.That(action.Result, Is.InstanceOf<OkObjectResult>());
        var ok = (OkObjectResult)action.Result!;
        var resultList = (List<AdminUserDto>)ok.Value!;
    
        Assert.That(resultList, Has.Count.EqualTo(2));
        
        var admin = resultList.First(u => u.Id == 1);
        Assert.That(admin.Username, Is.EqualTo("admin"));
        Assert.That(admin.Roles, Is.EquivalentTo(new[] { "Admin", "User" }));

        var user = resultList.First(u => u.Id == 2);
        Assert.That(user.Username, Is.EqualTo("user"));
        Assert.That(user.Roles, Is.EquivalentTo(new[] { "User" }));
    }
}
