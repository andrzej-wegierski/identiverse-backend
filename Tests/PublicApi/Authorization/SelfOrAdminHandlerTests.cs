using System.Security.Claims;
using Domain.Exceptions;
using Domain.Services;
using identiverse_backend.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Moq;
using NUnit.Framework;

namespace Tests.PublicApi.Authorization;

[TestFixture]
public class SelfOrAdminHandlerTests
{
    private Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private Mock<IAccessControlService> _accessControlServiceMock;
    private SelfOrAdminHandler _handler;

    [SetUp]
    public void SetUp()
    {
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _accessControlServiceMock = new Mock<IAccessControlService>();
        _handler = new SelfOrAdminHandler(_httpContextAccessorMock.Object, _accessControlServiceMock.Object);
    }

    private AuthorizationHandlerContext CreateContext(ClaimsPrincipal user, RouteData? routeData = null)
    {
        var requirement = new SelfOrAdminRequirement();
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        var httpContext = new DefaultHttpContext();
        if (routeData != null)
        {
            foreach (var kvp in routeData.Values)
            {
                httpContext.Request.RouteValues[kvp.Key] = kvp.Value;
            }
        }
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        return context;
    }

    [Test]
    public async Task HandleAsync_AdminUser_Succeeds()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, "Admin")
        }, "Test"));
        var context = CreateContext(user);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.That(context.HasSucceeded, Is.True);
        _accessControlServiceMock.Verify(x => x.CanAccessPersonAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_NonAdmin_WithValidId_AccessServiceSucceeds_Succeeds()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, "User")
        }, "Test"));
        
        var routeData = new RouteData();
        routeData.Values["id"] = "10";
        var context = CreateContext(user, routeData);

        _accessControlServiceMock.Setup(x => x.CanAccessPersonAsync(10, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.That(context.HasSucceeded, Is.True);
    }

    [Test]
    public async Task HandleAsync_NonAdmin_WithValidPersonId_AccessServiceSucceeds_Succeeds()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, "User")
        }, "Test"));
        
        var routeData = new RouteData();
        routeData.Values["personId"] = "20";
        var context = CreateContext(user, routeData);

        _accessControlServiceMock.Setup(x => x.CanAccessPersonAsync(20, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.That(context.HasSucceeded, Is.True);
    }

    [Test]
    [TestCase(typeof(ForbiddenException))]
    [TestCase(typeof(UnauthorizedIdentiverseException))]
    [TestCase(typeof(NotFoundException))]
    public async Task HandleAsync_NonAdmin_WithValidPersonId_AccessServiceThrows_DoesNotSucceed(Type exceptionType)
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, "User")
        }, "Test"));
        
        var routeData = new RouteData();
        routeData.Values["personId"] = "10";
        var context = CreateContext(user, routeData);

        var exception = (Exception)Activator.CreateInstance(exceptionType, "Access Denied")!;
        _accessControlServiceMock.Setup(x => x.CanAccessPersonAsync(10, It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.That(context.HasSucceeded, Is.False);
    }

    [Test]
    public async Task HandleAsync_NoPersonIdInRoute_DoesNotSucceed()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, "User")
        }, "Test"));
        
        var context = CreateContext(user, new RouteData());

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.That(context.HasSucceeded, Is.False);
    }
    
    [Test]
    public async Task HandleAsync_NoPersonIdInRoute_CallsFail()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "User") }, "Test"));
        var context = CreateContext(user, new RouteData());

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.That(context.HasFailed, Is.True);
    }

    [Test]
    public async Task HandleAsync_InvalidPersonIdInRoute_CallsFail()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "User") }, "Test"));
        var routeData = new RouteData();
        routeData.Values["id"] = "not-an-int";
        var context = CreateContext(user, routeData);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.That(context.HasFailed, Is.True);
    }

    [Test]
    [TestCase(typeof(ForbiddenException))]
    public async Task HandleAsync_AccessServiceThrows_CallsFail(Type exceptionType)
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "User") }, "Test"));
        var routeData = new RouteData();
        routeData.Values["id"] = "10";
        var context = CreateContext(user, routeData);

        var exception = (Exception)Activator.CreateInstance(exceptionType, "Access Denied")!;
        _accessControlServiceMock.Setup(x => x.CanAccessPersonAsync(10, It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.That(context.HasFailed, Is.True);
    }
}
